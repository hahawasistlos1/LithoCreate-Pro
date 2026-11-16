using System.Numerics;

namespace LithoCreate.Core;

/// <summary>Two perforated tabs stitched to the existing top border, without mesh booleans.</summary>
public static class HangingTabs
{
    public sealed record Tab(double MinX,double MaxX,Vector2[] Boundary);
    public sealed record Layout(double[] U,Tab[] Tabs);
    public static void Validate(ModelSettings s)
    {
        if(!new[]{s.TabWidth,s.TabHeight,s.TabHoleDiameter,s.TabInset}.All(double.IsFinite))throw new InvalidDataException("Aufhängelaschen: alle Maße müssen endliche Zahlen sein.");
        if(!s.SupportsHangingTabs||!s.HangingTabsEnabled)return;
        if(s.TabWidth is < 6 or > 40||s.TabHeight is < 6 or > 40||s.TabInset<0||s.TabInset>100)
            throw new InvalidDataException("Aufhängelaschen: Breite/Höhe 6–40 mm; Randabstand 0–100 mm.");
        if(s.TabHoleDiameter is < 2 or > 20||s.TabHoleDiameter>Math.Min(s.TabWidth,s.TabHeight)-2.5)
            throw new InvalidDataException("Laschenbohrung: 2–20 mm; Breite und Höhe müssen mindestens 2,5 mm größer sein. So bleiben auch an der polygonalen Lochwand mindestens 1,2 mm Material.");
        if(2*(s.TabInset+s.TabWidth)+2>s.Width)
            throw new InvalidDataException("Die beiden Aufhängelaschen passen nicht auf die Bildbreite. Mindestens 2 mm Abstand zwischen den Laschen lassen.");
        if(s.FrameWidth<1.2)throw new InvalidDataException("Für die Aufhängelaschen mindestens 1,2 mm Rahmenbreite einstellen.");
    }
    public static Layout CreateLayout(ModelSettings s,int nx,bool preview)
    {
        var us=Enumerable.Range(0,nx+1).Select(i=>i/(double)nx).ToList();var tabs=new List<Tab>();
        foreach(double left in new[]{-s.Width/2+s.TabInset,s.Width/2-s.TabInset-s.TabWidth})
        {
            double right=left+s.TabWidth,cx=(left+right)/2,cy=s.TabHeight/2;int segments=preview?32:64;
            var boundary=new List<Vector2>();
            foreach(var p in new[]{(left,0d),(right,0d),(left,s.TabHeight),(right,s.TabHeight)})Add(p.Item1,p.Item2);
            for(int i=0;i<segments;i++)
            {
                double a=i*Math.Tau/segments,dx=Math.Cos(a),dy=Math.Sin(a),tx=Math.Abs(dx)<1e-12?double.PositiveInfinity:s.TabWidth/(2*Math.Abs(dx)),ty=Math.Abs(dy)<1e-12?double.PositiveInfinity:s.TabHeight/(2*Math.Abs(dy)),t=Math.Min(tx,ty);
                double x=Math.Clamp(cx+dx*t,left,right),y=Math.Clamp(cy+dy*t,0,s.TabHeight);if(Math.Abs(y)<1e-8)y=0;if(Math.Abs(y-s.TabHeight)<1e-8)y=s.TabHeight;Add(x,y);
            }
            // Every original photo-grid vertex on the attachment must belong to the tab too.
            foreach(double u in us.ToArray()){double x=(u-.5)*s.Width;if(x>=left-1e-8&&x<=right+1e-8)Add(x,0);}
            var center=new Vector2((float)((left+right)/2),(float)(s.TabHeight/2));
            tabs.Add(new(left,right,boundary.OrderBy(p=>Math.Atan2(p.Y-center.Y,p.X-center.X)).ToArray()));
            void Add(double x,double y)
            {
                double baseY=s.Height+(s.StandEnabled?s.StandHeight:0);
                if((float)(baseY+y)==(float)baseY)y=0;
                if((float)(baseY+y)==(float)(baseY+s.TabHeight))y=s.TabHeight;
                var p=new Vector2((float)x,(float)y);if(!boundary.Contains(p))boundary.Add(p);if(y==0)us.Add(x/s.Width+.5);
            }
        }
        // Nearly coincident float coordinates otherwise create zero-width photo-grid cells.
        var sorted=us.Order().ToArray();var unique=new List<double>();foreach(double u in sorted)if(unique.Count==0||(u-unique[^1])*s.Width>.0001)unique.Add(u);
        if(unique[^1]!=1)unique[^1]=1;
        var gridX=unique.Select(u=>(float)((u-.5)*s.Width)).ToArray();
        var joined=tabs.Select(tab=>
        {
            var points=tab.Boundary.Select(p=>p.Y==0?new Vector2(gridX[Enumerable.Range(0,gridX.Length).MinBy(i=>Math.Abs(gridX[i]-p.X))],0):p).Distinct();
            return tab with{Boundary=points.OrderBy(p=>Math.Atan2(p.Y-s.TabHeight/2,p.X-(tab.MinX+tab.MaxX)/2)).ToArray()};
        }).ToArray();
        return new(unique.ToArray(),joined);
    }
    public static void Append(Mesh m,ModelSettings s,Layout layout,int[] frontTop,int[] backTop,CancellationToken token)
    {
        double y0=s.Height+(s.StandEnabled?s.StandHeight:0),thickness=s.MaxThickness+1;
        var ids=new Dictionary<(float X,float Y,float Z),int>();
        foreach(var tab in layout.Tabs)
        {
            token.ThrowIfCancellationRequested();var loop=tab.Boundary;double cx=(tab.MinX+tab.MaxX)/2,cy=s.TabHeight/2;
            var angles=loop.Select(p=>Math.Atan2(p.Y-cy,p.X-cx)).ToArray();double maxGap=Enumerable.Range(0,angles.Length).Max(i=>(angles[(i+1)%angles.Length]-angles[i]+Math.Tau)%Math.Tau);
            // Circumscribed polygon: the free diameter is never smaller than requested.
            double radius=s.TabHoleDiameter/(2*Math.Cos(maxGap/2))*(1+1e-7);
            int n=loop.Length;int[] outerFront=new int[n],outerBack=new int[n],innerFront=new int[n],innerBack=new int[n];
            for(int i=0;i<n;i++)
            {
                var p=loop[i];
                if(p.Y==0)
                {
                    int index=Enumerable.Range(0,layout.U.Length).MinBy(k=>Math.Abs((layout.U[k]-.5)*s.Width-p.X));
                    if(Math.Abs((layout.U[index]-.5)*s.Width-p.X)>.0001)throw new InvalidDataException("Laschenanschluss konnte nicht verbunden werden.");
                    outerFront[i]=frontTop[index];outerBack[i]=backTop[index];
                }
                else{outerFront[i]=V(p.X,y0+p.Y,thickness);outerBack[i]=V(p.X,y0+p.Y,0);}
                double x=cx+radius*Math.Cos(angles[i]),y=y0+cy+radius*Math.Sin(angles[i]);innerFront[i]=V(x,y,thickness);innerBack[i]=V(x,y,0);
            }
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                Q(outerFront[i],outerFront[j],innerFront[j],innerFront[i]);
                Q(outerBack[j],outerBack[i],innerBack[i],innerBack[j]);
                Q(innerFront[i],innerFront[j],innerBack[j],innerBack[i]);
                if(loop[i].Y!=0||loop[j].Y!=0)Q(outerFront[j],outerFront[i],outerBack[i],outerBack[j]);
            }
        }
        int V(double x,double y,double z){var key=((float)x,(float)y,(float)z);if(ids.TryGetValue(key,out int id))return id;id=m.Vertices.Count;ids.Add(key,id);m.Vertices.Add(new(key.Item1,key.Item2,key.Item3));m.TextureCoordinates.Add(new(.5f,.5f));m.PhotoVertices.Add(false);return id;}
        void Q(int a,int b,int c,int d){F(a,b,c);F(a,c,d);}
        void F(int a,int b,int c){if(a!=b&&a!=c&&b!=c)m.Face(a,b,c);}
    }
}
