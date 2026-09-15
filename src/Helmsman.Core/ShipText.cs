using System;
using System.Text;
namespace Helmsman.Core;
public static class ShipText
{
    public static string CleanName(string text)
    {
        var b=new StringBuilder();bool tag=false;
        foreach(char c in text??"")
        {
            if(c=='<'){tag=true;continue;}if(c=='>'){tag=false;continue;}
            if(!tag && !char.IsControl(c) && b.Length<48)b.Append(c);
        }
        return b.ToString().Trim();
    }
    public static string Bearing(double east,double north)
    {
        if(Math.Abs(east)+Math.Abs(north)<.01)return "Here";
        string[] points={"N","NE","E","SE","S","SW","W","NW"};
        double angle=(Math.Atan2(east,north)*180/Math.PI+360)%360;
        return points[(int)Math.Floor((angle+22.5)/45)%8];
    }
}
