using System.Collections.Generic;
public class Piece
{
 public enum UsageTagFlags{Misc=1,Decor=512}
 public string gameObject;
 public bool m_repairPiece,m_removePiece;
 public static implicit operator bool(Piece p)=>p!=null;
}
public class PieceTable {public HashSet<Piece> m_availablePieces=new();}
public class ByUsagePieceList
{
 public string GetTagDisplayName(int index)=>"";
 public void UpdateAvailableTags(PieceTable pieceTable){}
 public void GetAvailablePiecesWithTag(int tagId,PieceTable pieceTable,IList<Piece> resultOut){}
}
public static class Utils{public static string GetPrefabName(string name)=>name.Replace("(Clone)","");}
namespace Helmsman
{
 public static class Plugin{public const string DockPrefab="helmsman_dock_ward";}
 public static class ImportedHulls{public const string TablePrefab="CarpentersTable";}
}
