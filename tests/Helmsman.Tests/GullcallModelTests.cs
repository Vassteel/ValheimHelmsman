using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Helmsman.Core;
internal static class GullcallModelTests
{
    internal static void Run(Action<bool,string> check)
    {
        using var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("GullcallModel")!;
        using var memory=new MemoryStream();input.CopyTo(memory);var data=memory.ToArray();
        var parts=GullcallModel.Read(new MemoryStream(data));
        check(parts.Count==6 && parts.Sum(p=>p.Triangles.Length/3)==906,"Runtime whistle asset decodes all six parts and 906 triangles");
        check(parts.All(p=>p.Vertices.Length>=9 && p.Color.Length==4),"Every decoded part has nonempty geometry and color");
        void Reject(byte[] corrupt,string label)
        {
            bool rejected=false;
            try {GullcallModel.Read(new MemoryStream(corrupt));}catch(InvalidDataException){rejected=true;}catch(EndOfStreamException){rejected=true;}
            check(rejected,label);
        }
        Reject(Array.Empty<byte>(),"Empty model rejects before any Unity mesh creation");
        Reject(data.Take(data.Length-1).ToArray(),"Truncated model rejects before any Unity mesh creation");
        var bad=(byte[])data.Clone();bad[0]=0;Reject(bad,"Unknown model format is rejected");
        bad=(byte[])data.Clone();Array.Copy(BitConverter.GetBytes(0),0,bad,4,4);Reject(bad,"Zero model parts rejected explicitly");
        bad=(byte[])data.Clone();Array.Copy(BitConverter.GetBytes(-1),0,bad,bad.Length-4,4);Reject(bad,"Out-of-range triangle indices rejected");
        Reject(data.Concat(new byte[]{0}).ToArray(),"Trailing asset data rejected");
    }
}
