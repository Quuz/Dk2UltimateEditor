"""Decode pinned Russian SDT MPEG packets with installed FFmpeg DLLs.
Only AVPacket/AVFrame public leading fields are mapped (FFmpeg 5, x64).
"""
import argparse
from collections import Counter
import ctypes as C
import hashlib
import json
import os
from pathlib import Path
import struct
import time

ROOT = Path(__file__).resolve().parents[1]
P = C.c_void_p
I = C.c_int

class PacketPrefix(C.Structure):
    _fields_ = [('buf', P), ('pts', C.c_int64), ('dts', C.c_int64), ('data', P), ('size', I)]

class FramePrefix(C.Structure):
    _fields_ = [('data', P * 8), ('linesize', I * 8), ('extended_data', C.POINTER(P)),
                ('width', I), ('height', I), ('nb_samples', I), ('format', I)]

class DecodeError(RuntimeError):
    pass

class Decoder:
    def __init__(self, dll_dir):
        if C.sizeof(P) != 8 or os.name != 'nt':
            raise RuntimeError('Only Windows x64 FFmpeg5 public-prefix ABI supported')
        self.dll_dir = Path(dll_dir).resolve()
        self.dll_handle = os.add_dll_directory(str(self.dll_dir))
        self.util = C.CDLL(str(self.dll_dir / 'avutil-57.dll'))
        self.codec = C.CDLL(str(self.dll_dir / 'avcodec-59.dll'))
        def bind(lib, name, result, args):
            f = getattr(lib, name); f.restype = result; f.argtypes = args
            return f
        self.codec_version = bind(self.codec, 'avcodec_version', C.c_uint, [])()
        self.util_version = bind(self.util, 'avutil_version', C.c_uint, [])()
        if self.codec_version >> 16 != 59 or self.util_version >> 16 != 57:
            raise RuntimeError('Unsupported FFmpeg ABI versions')
        self.find = bind(self.codec, 'avcodec_find_decoder_by_name', P, [C.c_char_p])
        self.alloc = bind(self.codec, 'avcodec_alloc_context3', P, [P])
        self.open = bind(self.codec, 'avcodec_open2', I, [P, P, P])
        self.free = bind(self.codec, 'avcodec_free_context', None, [C.POINTER(P)])
        self.packet_alloc = bind(self.codec, 'av_packet_alloc', P, [])
        self.packet_free = bind(self.codec, 'av_packet_free', None, [C.POINTER(P)])
        self.packet_unref = bind(self.codec, 'av_packet_unref', None, [P])
        self.new_packet = bind(self.codec, 'av_new_packet', I, [P, I])
        self.send = bind(self.codec, 'avcodec_send_packet', I, [P, P])
        self.receive = bind(self.codec, 'avcodec_receive_frame', I, [P, P])
        self.frame_alloc = bind(self.util, 'av_frame_alloc', P, [])
        self.frame_unref = bind(self.util, 'av_frame_unref', None, [P])
        self.frame_free = bind(self.util, 'av_frame_free', None, [C.POINTER(P)])
        self.option = bind(self.util, 'av_opt_get_int', I, [P, C.c_char_p, I, C.POINTER(C.c_int64)])
        self.sample_name = bind(self.util, 'av_get_sample_fmt_name', C.c_char_p, [I])
        self.planar = bind(self.util, 'av_sample_fmt_is_planar', I, [I])
        self.sample_bytes = bind(self.util, 'av_get_bytes_per_sample', I, [I])
        self.strerror = bind(self.util, 'av_strerror', I, [I, C.c_char_p, C.c_size_t])
        self.decoder = self.find(b'mp2')
        if not self.decoder:
            raise RuntimeError('mp2 decoder unavailable')

    def describe(self):
        return dict(avcodec_version=self.codec_version, avutil_version=self.util_version,
                    dll_sha256={n: hashlib.sha256((self.dll_dir / n).read_bytes()).hexdigest()
                                for n in ['avcodec-59.dll', 'avutil-57.dll']},
                    packet_prefix_offsets={n: getattr(PacketPrefix, n).offset for n, _ in PacketPrefix._fields_},
                    frame_prefix_offsets={n: getattr(FramePrefix, n).offset for n, _ in FramePrefix._fields_})

    def error(self, value, stage):
        b = C.create_string_buffer(256)
        self.strerror(value, b, len(b))
        return DecodeError(f'{stage}: {value}: {b.value.decode("utf8", "replace")}')

    def int_option(self, ctx, name):
        value = C.c_int64()
        result = self.option(ctx, name.encode('ascii'), 0, C.byref(value))
        if result < 0:
            raise self.error(result, f'get_{name}')
        return value.value

    def decode(self, packets):
        ctx = P(self.alloc(self.decoder)); packet = P(); frame = P()
        pcm_hash = hashlib.sha256()
        frames = samples = pcm_bytes = 0
        rates, channels_seen, formats, per_frame_samples = Counter(), Counter(), Counter(), Counter()
        try:
            if not ctx.value:
                raise MemoryError('avcodec_alloc_context3')
            ret = self.open(ctx, self.decoder, None)
            if ret < 0: raise self.error(ret, 'open')
            packet = P(self.packet_alloc()); frame = P(self.frame_alloc())
            if not packet.value or not frame.value: raise MemoryError('packet/frame alloc')
            def receive_all(draining=False):
                nonlocal frames, samples, pcm_bytes
                while True:
                    ret = self.receive(ctx, frame)
                    if ret == -11:
                        if draining: raise DecodeError('EAGAIN while draining instead of EOF')
                        return False
                    if ret == -541478725:
                        return True
                    if ret < 0: raise self.error(ret, 'receive')
                    try:
                        f = C.cast(frame, C.POINTER(FramePrefix)).contents
                        rate = self.int_option(ctx, 'ar')
                        channels = self.int_option(ctx, 'ac')
                        size = self.sample_bytes(f.format)
                        if not (0 < f.nb_samples < 100000 and 0 < channels < 33 and 0 < rate < 500000 and 0 < size < 9):
                            raise DecodeError('implausible decoded frame ABI values')
                        name = self.sample_name(f.format)
                        if not name: raise DecodeError('unknown sample format')
                        planes = channels if self.planar(f.format) else 1
                        plane_size = f.nb_samples * size * (1 if planes > 1 or self.planar(f.format) else channels)
                        for plane in range(planes):
                            address = f.extended_data[plane]
                            if not address: raise DecodeError('null PCM plane')
                            pcm_hash.update(C.string_at(address, plane_size))
                        frames += 1; samples += f.nb_samples; pcm_bytes += plane_size * planes
                        rates[rate] += 1; channels_seen[channels] += 1
                        formats[name.decode()] += 1; per_frame_samples[f.nb_samples] += 1
                    finally:
                        self.frame_unref(frame)
            packet_count = 0
            for data in packets:
                ret = self.new_packet(packet, len(data))
                if ret < 0: raise self.error(ret, 'new_packet')
                try:
                    prefix = C.cast(packet, C.POINTER(PacketPrefix)).contents
                    if prefix.size != len(data) or not prefix.data: raise DecodeError('packet ABI mismatch')
                    C.memmove(prefix.data, data, len(data))
                    ret = self.send(ctx, packet)
                    if ret == -11:
                        receive_all()
                        ret = self.send(ctx, packet)
                    if ret < 0: raise self.error(ret, f'send_packet_{packet_count}')
                    receive_all(); packet_count += 1
                finally:
                    self.packet_unref(packet)
            ret = self.send(ctx, None)
            if ret < 0: raise self.error(ret, 'send_eof')
            eof = receive_all(draining=True)
            if not frames: raise DecodeError('decoder returned no frames')
            return dict(packets_sent=packet_count, decoded_frames=frames, decoded_samples_per_channel=samples,
                        decoded_pcm_bytes=pcm_bytes, decoded_pcm_sha256=pcm_hash.hexdigest(),
                        observed_rate_frame_counts=dict(rates), observed_channel_frame_counts=dict(channels_seen),
                        observed_sample_formats=dict(formats), observed_nb_samples_counts=dict(per_frame_samples),
                        drained_to_eof=eof)
        finally:
            if frame.value: self.frame_free(C.byref(frame))
            if packet.value: self.packet_free(C.byref(packet))
            if ctx.value: self.free(C.byref(ctx))

def inspect(root, decoder, baseline_path, limit=None, select_file=None, select_index=None):
    baseline_raw = baseline_path.read_bytes()
    baseline = json.loads(baseline_raw)
    rows = []; total_frames = total_samples = 0
    start = time.monotonic()
    for source in baseline['files']:
        if select_file and source['file'] != select_file: continue
        raw = (root / source['file']).read_bytes()
        if hashlib.sha256(raw).hexdigest() != source['sha256']: raise ValueError('source SHA mismatch')
        count, = struct.unpack_from('<I', raw)
        offsets = struct.unpack_from(f'<{count}I', raw, 4)
        if count != source['records'] or not all(a < b for a,b in zip(offsets,offsets[1:])): raise ValueError('bad directory')
        if not offsets or offsets[0] < 4 + count * 4: raise ValueError('bad directory extent')
        for index,(offset,end) in enumerate(zip(offsets,offsets[1:]+(len(raw),)),1):
            if select_index and select_index != index: continue
            header = struct.unpack_from('<II16sHBBIII', raw, offset)
            size, declared, _, rate, bits, kind, _, pcm, _ = header
            if size != 40 or declared != end-offset or bits !=16 or kind !=36: raise ValueError('not pinned length40 pattern')
            payload = raw[offset+size:end]
            if payload[-1:] != b'\0' or (len(payload)-1)%720: raise ValueError('not pinned framing')
            payload = payload[:-1]
            packets=[]
            for pos in range(0,len(payload),720):
                packet=payload[pos:pos+720]; h=int.from_bytes(packet[:4],'big')
                if h>>9 != 0xfffd98c4>>9 or (h>>6)&3 !=3: raise ValueError('bad MPEG header')
                packets.append(packet)
            decoded = decoder.decode(packets)
            row = dict(file=source['file'], index=index, source_sha256=source['sha256'],
                       nominal_sdt_rate_hz=rate, nominal_pcm_bytes=pcm,
                       header_implied_rate_hz=32000, header_implied_samples=len(packets)*1152, **decoded)
            rows.append(row); total_frames += decoded['decoded_frames']; total_samples += decoded['decoded_samples_per_channel']
            if limit and len(rows)>=limit: break
        if limit and len(rows)>=limit: break
    if not rows: raise ValueError('no selected clips')
    try:
        decoder.decode([bytes(720)])
    except DecodeError as error:
        negative_control=dict(malformed_packet='720 zero bytes, no valid MPEG header', rejected=True, error=str(error))
    else:
        raise RuntimeError('malformed packet negative control unexpectedly decoded')
    return dict(scope='Actual mp2 decoder output from pinned Russian length40 SDTs; original files read only',
                audio_decoded=True, game_playback_tested=False, game_files_written=0,
                baseline_sha256=hashlib.sha256(baseline_raw).hexdigest(), decoder=decoder.describe(),
                primary_abi_sources=['https://github.com/FFmpeg/FFmpeg/blob/n5.1.2/libavutil/frame.h',
                                     'https://github.com/FFmpeg/FFmpeg/blob/n5.1.2/libavcodec/packet.h',
                                     'https://ffmpeg.org/doxygen/5.1/group__lavc__encdec.html'],
                method='One physical 720-byte MPEG frame per avcodec packet; asserted final NUL excluded; decoder drained to EOF',
                pcm_hash_order='per decoded frame, PCM planes in channel order, no linesize padding',
                limits=dict(limit=limit, file=select_file, index=select_index), negative_control=negative_control,
                totals=dict(clips=len(rows),decoded_frames=total_frames,decoded_samples_per_channel=total_samples,
                            decoded_pcm_bytes=sum(x['decoded_pcm_bytes'] for x in rows),
                            decode_errors=0, all_drained_to_eof=all(x['drained_to_eof'] for x in rows),
                            clips_matching_header_implied_samples=sum(x['decoded_samples_per_channel']==x['header_implied_samples'] for x in rows),
                            clips_with_nominal_pcm_mismatch=sum(x['nominal_pcm_bytes']!=x['decoded_pcm_bytes'] for x in rows),
                            elapsed_seconds=time.monotonic()-start),
                limitations=['No game playback, no SDT/PCM output files, no interpretation of game-side nominal metadata',
                             'Successful FFmpeg decoding does not prove original audio perceptual fidelity or CRC correctness'],
                clips=rows)

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('root',type=Path); p.add_argument('--report',type=Path,required=True)
    p.add_argument('--limit',type=int); p.add_argument('--file'); p.add_argument('--index',type=int)
    p.add_argument('--dll-dir',type=Path,default=Path('C:/Program Files/FFmpeg For Audacity'))
    p.add_argument('--baseline',type=Path,default=ROOT/'sound-russian-sdt-length-audit.json')
    a=p.parse_args()
    if a.limit is not None and a.limit<1: p.error('--limit must be positive')
    if a.index is not None and (not a.file or a.index<1): p.error('--index requires --file and positive index')
    if a.report.suffix.lower()!='.json' or a.report.resolve().is_relative_to(a.root.resolve()): p.error('JSON report must be outside input root')
    report=inspect(a.root,Decoder(a.dll_dir),a.baseline,a.limit,a.file,a.index)
    a.report.write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
    print(json.dumps(report['totals']))

if __name__=='__main__': main()