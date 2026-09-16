using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Helmsman.Core;

// Explicit asset format: avoids Unity's native JSON deserializer silently dropping model arrays.
public static class GullcallModel
{
    public sealed class Part
    {
        public string Name="";
        public float[] Color=Array.Empty<float>(), Vertices=Array.Empty<float>();
        public int[] Triangles=Array.Empty<int>();
    }
    public static List<Part> Read(Stream stream)
    {
        using var reader=new BinaryReader(stream,Encoding.UTF8,true);
        if(reader.ReadUInt32()!=0x314C5547)throw new InvalidDataException("Invalid Gullcall model header.");
        int count=reader.ReadInt32();if(count<1 || count>32)throw new InvalidDataException("Invalid part count.");
        var parts=new List<Part>();
        for(int p=0;p<count;p++)
        {
            int bytes=reader.ReadUInt16();if(bytes==0 || bytes>256)throw new InvalidDataException("Invalid part name.");
            var name=reader.ReadBytes(bytes);if(name.Length!=bytes)throw new EndOfStreamException();
            var part=new Part {Name=Encoding.UTF8.GetString(name),Color=new float[4]};
            for(int i=0;i<4;i++)part.Color[i]=Finite(reader.ReadSingle());
            int vertices=reader.ReadInt32();if(vertices<3 || vertices>65535)throw new InvalidDataException("Invalid vertex count.");
            part.Vertices=new float[vertices*3];
            for(int i=0;i<part.Vertices.Length;i++)part.Vertices[i]=Finite(reader.ReadSingle());
            int triangles=reader.ReadInt32();if(triangles<3 || triangles>200000 || triangles%3!=0)throw new InvalidDataException("Invalid triangle count.");
            part.Triangles=new int[triangles];
            for(int i=0;i<triangles;i++)
            {int index=reader.ReadInt32();if(index<0 || index>=vertices)throw new InvalidDataException("Invalid triangle index.");part.Triangles[i]=index;}
            parts.Add(part);
        }
        if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected trailing model data.");
        return parts;
    }
    private static float Finite(float value)
    {if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidDataException("Invalid model coordinate.");return value;}
}
