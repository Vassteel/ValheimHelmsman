using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Helmsman.Core;

public sealed class ScoutPoint
{
    public readonly float X,Z;
    public readonly string Name;
    public ScoutPoint(float x,float z,string name){X=x;Z=z;Name=name;}
}
public sealed class ScoutReport
{
    public readonly HashSet<IslandCell> Cells;
    public readonly List<ScoutPoint> Points;
    public ScoutReport(IEnumerable<IslandCell> cells,IEnumerable<ScoutPoint> points)
    {Cells=new HashSet<IslandCell>(cells);Points=new List<ScoutPoint>(points);}
    public bool Contains(float x,float z)=>Cells.Contains(new IslandCell((int)Math.Round(x/IslandSurvey.CellSize),(int)Math.Round(z/IslandSurvey.CellSize)));
    public byte[] Encode()
    {
        using var output=new MemoryStream();
        using(var gzip=new GZipStream(output,CompressionLevel.Fastest,true))
        using(var writer=new BinaryWriter(gzip,Encoding.UTF8,true))
        {
            writer.Write(1);writer.Write(Cells.Count);
            foreach(var c in Cells){writer.Write(c.X);writer.Write(c.Z);}
            writer.Write(Points.Count);
            foreach(var p in Points)
            {
                writer.Write(p.X);writer.Write(p.Z);var bytes=Encoding.UTF8.GetBytes(p.Name);
                if(bytes.Length>128)throw new InvalidDataException("POI label is too long.");
                writer.Write(bytes.Length);writer.Write(bytes);
            }
        }
        return output.ToArray();
    }
    public static ScoutReport Decode(byte[] data)
    {
        if(data.Length>16*1024*1024)throw new InvalidDataException("Report is too large.");
        using var input=new MemoryStream(data);using var gzip=new GZipStream(input,CompressionMode.Decompress);using var reader=new BinaryReader(gzip);
        if(reader.ReadInt32()!=1)throw new InvalidDataException("Unknown survey report version.");
        int count=reader.ReadInt32();if(count<1||count>IslandSurvey.MaximumCells)throw new InvalidDataException("Invalid survey size.");
        var cells=new HashSet<IslandCell>();
        for(int i=0;i<count;i++)
        {
            int x=reader.ReadInt32(),z=reader.ReadInt32();
            if(Math.Abs((long)x)>1225||Math.Abs((long)z)>1225||!cells.Add(new IslandCell(x,z)))throw new InvalidDataException("Invalid survey cell.");
        }
        count=reader.ReadInt32();if(count<0||count>10000)throw new InvalidDataException("Invalid POI count.");
        var points=new List<ScoutPoint>();
        for(int i=0;i<count;i++)
        {
            float x=reader.ReadSingle(),z=reader.ReadSingle();int size=reader.ReadInt32();
            if(float.IsNaN(x)||float.IsNaN(z)||float.IsInfinity(x)||float.IsInfinity(z)||Math.Abs(x)>9800||Math.Abs(z)>9800||size<1||size>128)throw new InvalidDataException("Invalid POI.");
            var bytes=reader.ReadBytes(size);if(bytes.Length!=size)throw new EndOfStreamException();
            points.Add(new ScoutPoint(x,z,Encoding.UTF8.GetString(bytes)));
        }
        if(reader.BaseStream.ReadByte()!=-1)throw new InvalidDataException("Trailing survey data.");
        return new ScoutReport(cells,points);
    }
}
