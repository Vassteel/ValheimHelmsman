#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Helmsman.Structures
{
    [Serializable]
    public sealed class BuildPiece
    {
        public string Prefab;
        public string Data="";
        public bool HasScale,HasExtendedData;
        public float ScaleX=1,ScaleY=1,ScaleZ=1;
        public float X,Y,Z,Qx,Qy,Qz,Qw;
    }
    public readonly struct Point
    {
        public readonly float X,Z;
        public Point(float x,float z){X=x;Z=z;}
    }
    public sealed class Blueprint
    {
        public readonly List<BuildPiece> Pieces = new List<BuildPiece>();
        public Point[] Hull;
        public float Radius;
        public float Width,Depth,Height;
        public void ValidatePlacementSize()
        {if(Width>160||Depth>160||Height>128)throw new FormatException("Build exceeds the tool’s 160 m wide / 128 m tall placement limit.");}
        public static Blueprint Parse(string content)
        {
            if(content.Length>12_000_000)throw new FormatException("File is larger than 12 MB.");
            var result=new Blueprint();int lineNumber=0;
            bool blueprint=content.TrimStart('\uFEFF',' ','\r','\n').StartsWith("#",StringComparison.Ordinal);
            bool pieces=!blueprint;
            using(var reader=new StringReader(content.TrimStart('\uFEFF')))
            {
                string line;
                while((line=reader.ReadLine())!=null)
                {
                    lineNumber++;
                    if(string.IsNullOrWhiteSpace(line))continue;
                    if(blueprint&&line.StartsWith("#",StringComparison.Ordinal))
                    {pieces=line.Trim().Equals("#Pieces",StringComparison.OrdinalIgnoreCase);continue;}
                    if(!pieces)continue;
                    BuildPiece piece;
                    if(blueprint)
                    {
                        var f=Fields(line);
                        if(f.Count!=10&&f.Count!=13&&f.Count!=14)throw new FormatException($"Line {lineNumber}: unsupported blueprint piece fields ({f.Count}).");
                        piece=new BuildPiece{Prefab=f[0],X=Number(f[2],lineNumber),Y=Number(f[3],lineNumber),Z=Number(f[4],lineNumber),
                            Qx=Number(f[5],lineNumber),Qy=Number(f[6],lineNumber),Qz=Number(f[7],lineNumber),Qw=Number(f[8],lineNumber),Data=f[9]};
                        piece.HasExtendedData=f.Count==14&&f[13].Length>0;
                        if(f.Count>=13)
                        {
                            piece.HasScale=true;piece.ScaleX=Number(f[10],lineNumber);piece.ScaleY=Number(f[11],lineNumber);piece.ScaleZ=Number(f[12],lineNumber);
                            foreach(float scale in new[]{piece.ScaleX,piece.ScaleY,piece.ScaleZ})
                                if(scale<.01f||scale>20)throw new FormatException($"Line {lineNumber}: scale must be between 0.01 and 20.");
                        }
                    }
                    else
                    {
                        // Empty vbuild numeric fields mean zero: never collapse whitespace.
                        var f=line.Split(new[]{' '},StringSplitOptions.None);
                        if(f.Length!=8)throw new FormatException($"Line {lineNumber}: expected eight vbuild fields.");
                        piece=new BuildPiece{Prefab=f[0],Qx=Number(f[1],lineNumber),Qy=Number(f[2],lineNumber),Qz=Number(f[3],lineNumber),Qw=Number(f[4],lineNumber),X=Number(f[5],lineNumber),Y=Number(f[6],lineNumber),Z=Number(f[7],lineNumber)};
                    }
                    if(piece.Prefab.Length==0||piece.Prefab.Length>128)throw new FormatException($"Line {lineNumber}: invalid prefab name.");
                    double norm=Math.Sqrt(piece.Qx*piece.Qx+piece.Qy*piece.Qy+piece.Qz*piece.Qz+piece.Qw*piece.Qw);
                    if(norm<.9||norm>1.1)throw new FormatException($"Line {lineNumber}: invalid rotation.");
                    piece.Qx/=(float)norm;piece.Qy/=(float)norm;piece.Qz/=(float)norm;piece.Qw/=(float)norm;
                    if(Math.Abs(piece.X)>10000||Math.Abs(piece.Y)>10000||Math.Abs(piece.Z)>10000)throw new FormatException($"Line {lineNumber}: position out of range.");
                    result.Pieces.Add(piece);
                    if(result.Pieces.Count>30000)throw new FormatException("Limit: 30,000 pieces per placement.");
                }
            }
            if(result.Pieces.Count==0)throw new FormatException("No pieces in this file.");
            result.Rebase();
            return result;
        }
        public int RetainPieces(Func<BuildPiece,bool> supported)
        {
            int skipped=Pieces.RemoveAll(p=>!supported(p));
            if(Pieces.Count==0)throw new FormatException("No supported building pieces remain in this file.");
            Rebase();
            return skipped;
        }
        private void Rebase()
        {
            var result=this;
            result.Radius=0;
            float minX=result.Pieces.Min(p=>p.X),maxX=result.Pieces.Max(p=>p.X),minZ=result.Pieces.Min(p=>p.Z),maxZ=result.Pieces.Max(p=>p.Z),minY=result.Pieces.Min(p=>p.Y);
            result.Width=maxX-minX;result.Depth=maxZ-minZ;result.Height=result.Pieces.Max(p=>p.Y)-minY;
            foreach(var p in result.Pieces){p.X-=(minX+maxX)/2;p.Z-=(minZ+maxZ)/2;p.Y-=minY;result.Radius=Math.Max(result.Radius,(float)Math.Sqrt(p.X*p.X+p.Z*p.Z)+3);}
            // Occupied lower layer determines the foundation, not a circular radius.
            result.Hull=Geometry.Hull(result.Pieces.Where(p=>p.Y<=2f).SelectMany(p=>new[]{new Point(p.X-1.5f,p.Z-1.5f),new Point(p.X+1.5f,p.Z-1.5f),new Point(p.X-1.5f,p.Z+1.5f),new Point(p.X+1.5f,p.Z+1.5f)}));
        }
        private static float Number(string value,int line)
        {
            if(value.Length==0)return 0;
            if((value.Contains(",")&&value.Contains("."))||!float.TryParse(value.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||float.IsNaN(n)||float.IsInfinity(n))throw new FormatException($"Line {line}: invalid number.");
            return n;
        }
        private static List<string> Fields(string line)
        {
            var result=new List<string>();var field=new System.Text.StringBuilder();bool quoted=false;
            for(int i=0;i<line.Length;i++)
            {
                char c=line[i];
                if(c=='"'&&(quoted||field.Length==0))
                {
                    if(quoted&&i+1<line.Length&&line[i+1]=='"'){field.Append('"');i++;}
                    else quoted=!quoted;
                }
                else if(c==';'&&!quoted){result.Add(field.ToString());field.Clear();}
                else field.Append(c);
            }
            if(quoted)throw new FormatException("Unclosed quoted blueprint field.");
            result.Add(field.ToString());return result;
        }
    }
    public static class Geometry
    {
        public const float RotationStep=22.5f;
        public static float Rotate(float yaw,int direction)
        {
            int step=(int)Math.Round(yaw/RotationStep)+direction;
            return ((step%16+16)%16)*RotationStep;
        }
        private static float Cross(Point a,Point b,Point c)=>(b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X);
        public static Point[] Hull(IEnumerable<Point> input)
        {
            var p=input.GroupBy(v=>(v.X,v.Z)).Select(g=>g.First()).OrderBy(v=>v.X).ThenBy(v=>v.Z).ToArray();
            if(p.Length<3)throw new ArgumentException("A footprint needs three distinct points.");
            var h=new List<Point>();
            foreach(var v in p){while(h.Count>=2&&Cross(h[h.Count-2],h[h.Count-1],v)<=0)h.RemoveAt(h.Count-1);h.Add(v);}
            int lower=h.Count;
            for(int i=p.Length-2;i>=0;i--){while(h.Count>lower&&Cross(h[h.Count-2],h[h.Count-1],p[i])<=0)h.RemoveAt(h.Count-1);h.Add(p[i]);}
            h.RemoveAt(h.Count-1);return h.ToArray();
        }
        public static float Distance(Point p,Point[] hull)
        {
            bool inside=true;float distance=float.MaxValue;
            for(int i=0;i<hull.Length;i++)
            {
                var a=hull[i];var b=hull[(i+1)%hull.Length];if(Cross(a,b,p)<0)inside=false;
                float x=b.X-a.X,z=b.Z-a.Z,den=x*x+z*z;
                float t=den==0?0:Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Z-a.Z)*z)/den));
                float dx=p.X-a.X-t*x,dz=p.Z-a.Z-t*z;
                distance=Math.Min(distance,(float)Math.Sqrt(dx*dx+dz*dz));
            }
            return inside?0:distance;
        }
        public static float BlendWeight(float distance,float width)
        {
            if(width<=0)throw new ArgumentOutOfRangeException(nameof(width));
            float t=Math.Max(0,Math.Min(1,distance/width));
            return 1-t*t*t*(t*(t*6-15)+10); // quintic: zero slope and curvature at both ends
        }
        // Leave a shallow soil overlap with the foundation. Preserve small natural
        // variations instead of cutting every point down to the lowest piece pivot.
        public static float FoundationHeight(float oldHeight,float foundation,float distance,float width)
        {
            float contact=Math.Max(foundation+.2f,Math.Min(foundation+.35f,oldHeight));
            return Height(oldHeight,contact,distance,width);
        }
        public static float Height(float oldHeight,float target,float distance,float width)=>oldHeight+(target-oldHeight)*BlendWeight(distance,width);
    }
}
