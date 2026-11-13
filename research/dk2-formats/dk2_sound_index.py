"""Read DK2 sound directory metadata without extracting or rewriting audio."""
from dataclasses import dataclass
from struct import Struct, unpack_from

BANK_GUID = bytes.fromhex("012c61e9d031d211b40900a0c993f203")
SFX_GUID = bytes.fromhex("002c61e9d031d211b40900b0c993f203")


class SoundFormatError(ValueError):
    pass


class Cursor:
    def __init__(self, data, offset=0):
        self.data = data
        self.offset = offset

    def take(self, size):
        start = self.offset
        self.offset += size
        if self.offset > len(self.data):
            raise SoundFormatError(f"Truncated at {start:#x}, need {size} bytes")
        return self.data[start:self.offset]

    def array(self, count, size):
        if count > (len(self.data) - self.offset) // size:
            raise SoundFormatError(f"Count {count} at {self.offset:#x} exceeds remaining data")
        return [self.take(size) for _ in range(count)]

    def finish(self):
        if self.offset != len(self.data):
            raise SoundFormatError(f"{len(self.data) - self.offset} trailing bytes")


def text(raw):
    return raw.split(b"\0", 1)[0].decode("latin-1")


def read_bank(data):
    cursor = Cursor(data)
    header = cursor.take(28)
    if header[:16] != BANK_GUID:
        raise SoundFormatError("Unexpected BANK.map signature")
    count, = unpack_from("<I", header, 24)
    table = cursor.array(count, 11)
    names = []
    for _ in table:
        size, = unpack_from("<I", cursor.take(4))
        name = cursor.take(size)
        if not name.endswith(b"\0"):
            raise SoundFormatError("Archive name lacks its final NUL")
        names.append(name)
    cursor.finish()
    return header, list(zip(table, names))


@dataclass(frozen=True)
class Item:
    offset: int
    raw: bytes
    sounds: list
    links: list


@dataclass(frozen=True)
class Group:
    offset: int
    raw: bytes
    items: list

    @property
    def id(self):
        return unpack_from("<I", self.raw)[0]


def read_sfx(data):
    cursor = Cursor(data)
    header = cursor.take(28)
    if header[:16] != SFX_GUID:
        raise SoundFormatError("Unexpected SFX.map signature")
    count, = unpack_from("<I", header, 24)
    entries = cursor.array(count, 24)
    directory = []
    for entry in entries:
        count, = unpack_from("<I", entry)
        start = cursor.offset
        directory.append([(start + 20 * index, row) for index, row in enumerate(cursor.array(count, 20))])
    groups = []
    for entry_groups in directory:
        for offset, row in entry_groups:
            count, = unpack_from("<I", row, 4)
            start = cursor.offset
            headers = cursor.array(count, 42)
            items = []
            for index, raw in enumerate(headers):
                sounds, links = unpack_from("<2I", raw)
                items.append(Item(start + index * 42, raw, cursor.array(sounds, 16), cursor.array(links, 8)))
            groups.append(Group(offset, row, items))
    cursor.finish()
    return header, entries, groups


SDT_HEADER = Struct("<II16sHBBIII")


@dataclass(frozen=True)
class Clip:
    offset: int
    header_size: int
    data_size: int
    name: str
    sample_rate: int
    sample_bits: int
    type: int
    word_1c: int
    pcm_bytes: int
    word_24: int
    prefix: str


def read_sdt(data):
    cursor = Cursor(data)
    count, = unpack_from("<I", cursor.take(4))
    directory = cursor.array(count, 4)
    clips = []
    for raw_offset in directory:
        offset, = unpack_from("<I", raw_offset)
        if not cursor.offset <= offset <= len(data) - SDT_HEADER.size:
            raise SoundFormatError(f"Entry header out of range at {offset:#x}")
        size, payload, name, rate, bits, kind, word_1c, pcm, word_24 = SDT_HEADER.unpack_from(data, offset)
        if size < SDT_HEADER.size or offset + size + payload > len(data):
            raise SoundFormatError(f"Entry extent out of range at {offset:#x}")
        start = offset + size
        clips.append(Clip(offset, size, payload, text(name), rate, bits, kind, word_1c, pcm, word_24,
                          data[start:start + min(4, payload)].hex()))
    return clips

