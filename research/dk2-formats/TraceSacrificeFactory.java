import ghidra.app.script.GhidraScript;
import ghidra.app.emulator.EmulatorHelper;
import ghidra.program.model.address.Address;
import com.google.gson.*;
import java.nio.file.*;
import java.util.*;

/** Bounded diagnostic: no changes to the Program or native instructions. */
public class TraceSacrificeFactory extends GhidraScript {
    private static final String ROOT="D:/Games/DK2/Tools/dk2-format-research/";
    private static long number(String v) { return v.startsWith("0x") ? Long.parseUnsignedLong(v.substring(2),16) : Long.parseLong(v); }
    private static String hex(byte[] b) { StringBuilder s=new StringBuilder();for(byte v:b)s.append(String.format("%02x",v&255));return s.toString(); }
    @Override public void run() throws Exception {
        JsonObject saved=JsonParser.parseString(Files.readString(Path.of(ROOT+"ghidra/phase89-sacrifice-object-object-page0.json"))).getAsJsonObject();
        JsonObject input=saved.getAsJsonObject("input");
        EmulatorHelper e=new EmulatorHelper(currentProgram);
        try {
            e.writeRegister("ESP",0x7fff0000L);e.writeRegister("EBP",0x7fff0000L);
            JsonObject registers=JsonParser.parseString(input.get("registers").getAsString()).getAsJsonObject();
            for(Map.Entry<String,JsonElement> r:registers.entrySet())e.writeRegister(r.getKey(),number(r.getValue().getAsString()));
            JsonArray regions=JsonParser.parseString(input.get("memory").getAsString()).getAsJsonObject().getAsJsonArray("regions");
            for(JsonElement element:regions) {
                JsonObject r=element.getAsJsonObject();String h=r.get("hex").getAsString();byte[] b=new byte[h.length()/2];
                for(int i=0;i<b.length;i++)b[i]=(byte)Integer.parseInt(h.substring(2*i,2*i+2),16);
                e.writeMemory(toAddr(number(r.get("address").getAsString())),b);
            }
            e.writeRegister(currentProgram.getLanguage().getProgramCounter().getName(),number(input.get("address").getAsString()));
            ArrayDeque<Map<String,String>> last=new ArrayDeque<>();int steps=0;boolean ok=true;
            for(;steps<50000;steps++) {
                Address pc=e.getExecutionAddress();if(pc.getOffset()==0xdeadbeefL)break;
                Map<String,String> row=new LinkedHashMap<>();row.put("pc",pc.toString());
                for(String reg:new String[]{"ESP","EAX","ECX","EDX","ESI","EDI","EBP"})row.put(reg,"0x"+e.readRegister(reg).toString(16));
                try {row.put("bytes",hex(e.readMemory(pc,12)));}catch(Exception ignored){}
                last.add(row);if(last.size()>300)last.removeFirst();
                if(!e.step(monitor)){ok=false;break;}
            }
            Map<String,Object> result=new LinkedHashMap<>();result.put("steps",steps);result.put("step_ok",ok);result.put("final_pc",e.getExecutionAddress().toString());result.put("error",e.getLastError());result.put("trace",last);
            result.put("object_hex",hex(e.readMemory(toAddr(0x10016074L),393)));
            result.put("stack_hex",hex(e.readMemory(toAddr(e.readRegister("ESP").longValue()),128)));
            Files.writeString(Path.of(ROOT+"phase89-object-temple-step-trace.json"),new GsonBuilder().setPrettyPrinting().create().toJson(result));
            println("Bounded trace written; steps="+steps+" PC="+e.getExecutionAddress()+" error="+e.getLastError());
        } finally {e.dispose();}
    }
}
