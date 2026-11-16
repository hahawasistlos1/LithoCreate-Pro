using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using LithoCreate.Core;

namespace LithoCreate.App.Controls;
public sealed class ModelViewport : Grid
{
    readonly Viewport3D viewport=new();
    readonly PerspectiveCamera camera=new(){FieldOfView=38,NearPlaneDistance=.1,FarPlaneDistance=20000};
    readonly Model3DGroup scene=new();
    ImageBrush? transmissionBrush;
    Mesh? mesh;GrayImage? image;
    double yaw=18,pitch=8,distance=350,panX,panY;
    bool autoFit=true;
    Point last;MouseButton? dragging;
    public bool Lit { get; private set; }=true;
    public bool GridVisible { get; private set; }=true;
    public bool Wireframe { get; private set; }
    public double Intensity { get; private set; }=.8;
    public ModelViewport()
    {
        ClipToBounds=true;Background=new RadialGradientBrush(Color.FromRgb(32,48,61),Color.FromRgb(5,12,20)) { RadiusX=.8,RadiusY=.8 };
        Children.Add(viewport);viewport.Camera=camera;viewport.Children.Add(new ModelVisual3D{Content=scene});
        SizeChanged+=(_,_)=>{if(autoFit)FitCamera();else UpdateCamera();};
        MouseDown+=(s,e)=>{autoFit=false;Focus();last=e.GetPosition(this);dragging=e.ChangedButton;CaptureMouse();e.Handled=true;};
        MouseUp+=(s,e)=>{dragging=null;ReleaseMouseCapture();};
        MouseMove+=(s,e)=> { if(dragging==null)return;var p=e.GetPosition(this);double dx=p.X-last.X,dy=p.Y-last.Y;last=p;if(dragging==MouseButton.Right || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)){panX-=dx*distance/900;panY+=dy*distance/900;}else {yaw+=dx*.4;pitch=Math.Clamp(pitch+dy*.35,-85,85);}UpdateCamera();};
        MouseWheel+=(s,e)=> { autoFit=false;distance=Math.Clamp(distance*Math.Pow(.88,e.Delta/120.0),10,8000);UpdateCamera();e.Handled=true;};
        Focusable=true;
        ToolTip="Linke Maustaste: drehen · Rechte Maustaste oder Umschalt: verschieben · Mausrad: zoomen";
    }
    public void SetMesh(Mesh value,GrayImage? gray,bool fit=false)
    {
        bool first=mesh==null;mesh=value;image=gray;Rebuild();if(first||fit)View("reset");else if(autoFit)FitCamera();else UpdateCamera();
    }
    public void Clear(){mesh=null;image=null;transmissionBrush=null;scene.Children.Clear();}
    public void View(string mode)
    {
        if(mesh==null)return;
        if(mode=="reset") {var bounds=mesh.Bounds();var size=bounds.Max-bounds.Min;bool flat=size.Y<.35*Math.Max(size.X,size.Z);yaw=!flat&&size.X<.35*Math.Min(size.Y,size.Z)?72:18;pitch=flat?55:8;panX=panY=0;}
        if(mode is "reset" or "front" or "back" or "side" or "top" or "bottom")autoFit=true;
        if(mode=="front"){yaw=0;pitch=0;panX=panY=0;}
        if(mode=="back"){yaw=180;pitch=0;panX=panY=0;}
        if(mode=="side"){yaw=90;pitch=0;panX=panY=0;}
        if(mode=="top"){yaw=0;pitch=89;panX=panY=0;}
        if(mode=="bottom"){yaw=0;pitch=-89;panX=panY=0;}
        if(mode=="grid"){GridVisible=!GridVisible;Rebuild();}
        if(mode=="light"){Lit=!Lit;Rebuild();}
        if(mode=="wire"){Wireframe=!Wireframe;Rebuild();}
        if(mode=="in"){autoFit=false;distance*=.8;}
        if(mode=="out"){autoFit=false;distance*=1.25;}
        if(autoFit)FitCamera();else UpdateCamera();
    }
    public void SetIntensity(double value){Intensity=double.IsFinite(value)?Math.Clamp(value,0,1):.8;if(transmissionBrush!=null)transmissionBrush.Opacity=Intensity;}
    void FitCamera()
    {
        if(mesh==null)return;var bounds=mesh.Bounds();
        var center=(bounds.Min+bounds.Max)/2;
        double a=yaw*Math.PI/180,p=pitch*Math.PI/180;
        var backward=new Vector3D(Math.Sin(a)*Math.Cos(p),Math.Sin(p),Math.Cos(a)*Math.Cos(p));
        var right=new Vector3D(Math.Cos(a),0,-Math.Sin(a));var up=Vector3D.CrossProduct(backward,right);
        double horizontal=Math.Tan(camera.FieldOfView*Math.PI/360);
        double vertical=horizontal*Math.Max(1,ActualHeight)/Math.Max(1,ActualWidth),required=0;
        foreach(float x in new[]{bounds.Min.X,bounds.Max.X})foreach(float y in new[]{bounds.Min.Y,bounds.Max.Y})foreach(float z in new[]{bounds.Min.Z,bounds.Max.Z})
        {
            var delta=new Vector3D(x-center.X,y-center.Y,z-center.Z);
            required=Math.Max(required,Vector3D.DotProduct(delta,backward)+Math.Max(Math.Abs(Vector3D.DotProduct(delta,right))/horizontal,Math.Abs(Vector3D.DotProduct(delta,up))/vertical));
        }
        distance=Math.Max(10,required*1.18+2);UpdateCamera();
    }
    void UpdateCamera()
    {
        if(mesh==null)return;var b=mesh.Bounds();
        var target=new Point3D((b.Min.X+b.Max.X)/2+panX,(b.Min.Y+b.Max.Y)/2+panY,(b.Min.Z+b.Max.Z)/2);
        double y=yaw*Math.PI/180,p=pitch*Math.PI/180;
        var offset=new Vector3D(distance*Math.Sin(y)*Math.Cos(p),distance*Math.Sin(p),distance*Math.Cos(y)*Math.Cos(p));
        camera.Position=target+offset;camera.LookDirection=-offset;camera.UpDirection=new(0,1,0);
    }
    void Rebuild()
    {
        transmissionBrush=null;scene.Children.Clear();if(mesh==null)return;
        scene.Children.Add(new AmbientLight(Color.FromRgb(125,125,135)));scene.Children.Add(new DirectionalLight(Colors.White,new(-.5,-1,-2)));
        var geometry=new MeshGeometry3D();var b=mesh.Bounds();
        for(int i=0;i<mesh.Vertices.Count;i++) { var v=mesh.Vertices[i];geometry.Positions.Add(new(v.X,v.Y,v.Z));var uv=mesh.TextureCoordinates.Count==mesh.Vertices.Count?mesh.TextureCoordinates[i]:new System.Numerics.Vector2((v.X-b.Min.X)/Math.Max(.001f,b.Max.X-b.Min.X),1-(v.Y-b.Min.Y)/Math.Max(.001f,b.Max.Y-b.Min.Y));geometry.TextureCoordinates.Add(new(uv.X,uv.Y)); }
        var structure=new MeshGeometry3D{Positions=geometry.Positions};
        foreach(var t in mesh.Triangles)
        {
            bool solid=mesh.PhotoVertices.Count==mesh.Vertices.Count&&(!mesh.PhotoVertices[t.A]||!mesh.PhotoVertices[t.B]||!mesh.PhotoVertices[t.C]);
            var target=solid?structure:geometry;
            if(!solid&&mesh.WrapTextureU)
            {
                double min=Math.Min(geometry.TextureCoordinates[t.A].X,Math.Min(geometry.TextureCoordinates[t.B].X,geometry.TextureCoordinates[t.C].X));
                double max=Math.Max(geometry.TextureCoordinates[t.A].X,Math.Max(geometry.TextureCoordinates[t.B].X,geometry.TextureCoordinates[t.C].X));
                if(max-min>.5)
                {
                    foreach(int index in new[]{t.A,t.B,t.C})
                    {
                        var uv=geometry.TextureCoordinates[index];if(uv.X<.5)uv.X+=1;
                        target.TriangleIndices.Add(geometry.Positions.Count);geometry.Positions.Add(geometry.Positions[index]);geometry.TextureCoordinates.Add(uv);
                    }
                    continue;
                }
            }
            target.TriangleIndices.Add(t.A);target.TriangleIndices.Add(t.B);target.TriangleIndices.Add(t.C);
        }
        geometry.Freeze();structure.Freeze();
        Material material;
        if(Lit&&image!=null&&geometry.TriangleIndices.Count>0)
        {
            var pixels=new byte[image.Width*image.Height*4];
            for(int i=0;i<image.Pixels.Length;i++) { double l=.87*Math.Pow(image.Pixels[i]/255.0,.65);pixels[4*i]=(byte)(l*125);pixels[4*i+1]=(byte)(l*202);pixels[4*i+2]=(byte)(l*255);pixels[4*i+3]=255; }
            var bitmap=System.Windows.Media.Imaging.BitmapSource.Create(image.Width,image.Height,96,96,PixelFormats.Bgra32,null,pixels,image.Width*4);bitmap.Freeze();
            // Emission is a visual approximation of transmitted light; mesh thickness remains physical.
            transmissionBrush=new ImageBrush(bitmap){TileMode=TileMode.Tile,Opacity=Intensity};
            var group=new MaterialGroup();group.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(107,78,40))));group.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(16,26,33))));group.Children.Add(new EmissiveMaterial(transmissionBrush));material=group;
        }
        else material=new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(222,230,236)));
        if(transmissionBrush==null)material.Freeze();scene.Children.Add(new GeometryModel3D(geometry,material){BackMaterial=material});
        if(structure.TriangleIndices.Count>0)
        {
            // Explicit face normals preserve shoulders and bores instead of smoothing
            // structural edges into a visually rounded surface. Export geometry is unchanged.
            var crisp=new MeshGeometry3D();
            for(int i=0;i<structure.TriangleIndices.Count;i+=3)
            {
                var pa=structure.Positions[structure.TriangleIndices[i]];var pb=structure.Positions[structure.TriangleIndices[i+1]];var pc=structure.Positions[structure.TriangleIndices[i+2]];var normal=Vector3D.CrossProduct(pb-pa,pc-pa);if(normal.Length>0)normal.Normalize();
                int start=crisp.Positions.Count;crisp.Positions.Add(pa);crisp.Positions.Add(pb);crisp.Positions.Add(pc);for(int n=0;n<3;n++){crisp.TriangleIndices.Add(start+n);crisp.Normals.Add(normal);}
            }
            crisp.Freeze();var plastic=new DiffuseMaterial(new SolidColorBrush(geometry.TriangleIndices.Count==0?Color.FromRgb(181,195,207):Lit?Color.FromRgb(106,94,76):Color.FromRgb(222,230,236)));plastic.Freeze();
            scene.Children.Add(new GeometryModel3D(crisp,plastic){BackMaterial=plastic});
        }
        if(GridVisible)
        {
            double span=Math.Max(150,Math.Max(b.Max.X-b.Min.X,b.Max.Z-b.Min.Z)*1.5);double y=b.Min.Y-.8;
            var grid=new MeshGeometry3D();
            for(double i=-span;i<=span;i+=10) { Strip(new(i,y,-span),new(i,y,span),.16);Strip(new(-span,y,i),new(span,y,i),.16); }
            var mat=new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(43,69,86)));scene.Children.Add(new GeometryModel3D(grid,mat){BackMaterial=mat});
            void Strip(Point3D a,Point3D c,double width) { var d=c-a;d.Normalize();var n=Vector3D.CrossProduct(d,new(0,1,0))*width;int k=grid.Positions.Count;grid.Positions.Add(a+n);grid.Positions.Add(c+n);grid.Positions.Add(c-n);grid.Positions.Add(a-n);int[] ids=[k,k+1,k+2,k,k+2,k+3];foreach(var id in ids)grid.TriangleIndices.Add(id); }
        }
        if(Wireframe)
        {
            var lines=new MeshGeometry3D();int stride=Math.Max(1,mesh.Triangles.Count/5000);
            for(int i=0;i<mesh.Triangles.Count;i+=stride) { var t=mesh.Triangles[i];Edge(t.A,t.B);Edge(t.B,t.C);Edge(t.C,t.A); }
            var mat=new EmissiveMaterial(Brushes.DeepSkyBlue);scene.Children.Add(new GeometryModel3D(lines,mat){BackMaterial=mat});
            void Edge(int a,int c)
            {
                var va=mesh.Vertices[a];var vc=mesh.Vertices[c];var start=new Point3D(va.X,va.Y,va.Z+.05);var end=new Point3D(vc.X,vc.Y,vc.Z+.05);var delta=end-start;
                var n=Vector3D.CrossProduct(delta,new(0,0,1));if(n.Length<.001)n=Vector3D.CrossProduct(delta,new(0,1,0));if(n.Length<.001)return;n.Normalize();n*=.045;
                int k=lines.Positions.Count;lines.Positions.Add(start+n);lines.Positions.Add(end+n);lines.Positions.Add(end-n);lines.Positions.Add(start-n);foreach(int id in new[]{k,k+1,k+2,k,k+2,k+3})lines.TriangleIndices.Add(id);
            }
        }
    }
}
