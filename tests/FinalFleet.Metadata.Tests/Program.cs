using System.IO.Compression;
using Helmsman;
using Newtonsoft.Json.Linq;

var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
int count=0;
foreach(var path in Directory.GetFiles(Path.Combine(root,"assets/ships/final"),"*.bin.gz"))
{
    using var zip=new GZipStream(File.OpenRead(path),CompressionMode.Decompress);
    using var reader=new BinaryReader(zip);
    if(new string(reader.ReadChars(4))!="HMF1")throw new Exception("Bad magic");
    var json=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    var name=Path.GetFileName(path).Replace(".bin.gz","");
    var spec=FinalFleetModels.ReadSpecification(json,name);
    if(spec.points.Length<6||spec.hullSolids.Length!=18||spec.colliders.Length==0||spec.sailPivot.Length!=3||spec.rudderPivot.Length!=3)throw new Exception("Lost geometry fields: "+name);
    if(spec.points.Any(p=>p.position.Length!=3||p.exit.Length!=3||p.facing.Length!=3))throw new Exception("Lost anchor fields: "+name);
    if(spec.name is "ottar" or "freighter")
        if(spec.cargoSolids.Length==0||spec.cargoHeight<=0)throw new Exception("Lost cargo fields: "+name);
    void Reject(string input,string expected)
    {
        try{FinalFleetModels.ReadSpecification(input,expected);}catch(InvalidDataException){return;}
        throw new Exception("Invalid specification accepted: "+name);
    }
    Reject(json,"WrongPrefab");
    var invalid=JObject.Parse(json);invalid["points"]=new JArray();Reject(invalid.ToString(),name);
    invalid=JObject.Parse(json);invalid["length"]=0;Reject(invalid.ToString(),name);
    invalid=JObject.Parse(json);invalid["beam"]=double.NaN;Reject(invalid.ToString(),name);
    invalid=JObject.Parse(json);invalid["points"]=null;Reject(invalid.ToString(),name);
    count++;Console.WriteLine("PASS: production metadata loader: "+name);
}
if(count!=6)throw new Exception("Missing fleet assets");
Console.WriteLine("PASS: all six specifications decoded with the installed game's Newtonsoft.Json; malformed specifications rejected.");
