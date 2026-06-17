<#
.SYNOPSIS
  Regenerate the README screenshot (screenshot.png) from the live app.

.DESCRIPTION
  Launches the app in --demo mode (an isolated, read-only instance with placeholder
  AAA profiles — it never touches the user's real config), captures its window with
  PrintWindow (DPI-correct), then composites a gradient backdrop, purple glow, a subtle
  3D perspective tilt, a drop shadow, and the title/tagline.

  Build first:
    cd Sonar.AutoSwitch
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

  Then from the repo root:
    powershell -ExecutionPolicy Bypass -File scripts\generate-screenshot.ps1
#>
param(
    [string]$Exe,
    [string]$Out,
    [double]$Tilt  = 0.075,
    [string]$Title = "SONAR AUTOSWITCH",
    [string]$Tag   = "Auto-switches your Sonar EQ per game"
)

$repo = Split-Path $PSScriptRoot -Parent
if (-not $Exe) { $Exe = Join-Path $repo "Sonar.AutoSwitch\bin\Release\net8.0\win-x64\publish\Sonar.AutoSwitch.exe" }
if (-not $Out) { $Out = Join-Path $repo "screenshot.png" }
$Raw = Join-Path $env:TEMP "sonar-raw.png"

if (-not (Test-Path $Exe)) {
    Write-Host "Build not found: $Exe"
    Write-Host "Build it first:  cd Sonar.AutoSwitch; dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true"
    exit 1
}

# ---------- capture ----------
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class WinCap
{
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int n);
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr ctx);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT r, int sz);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static void Dpi() { SetThreadDpiAwarenessContext(new IntPtr(-4)); }
    public static string Capture(IntPtr hwnd, string outPath)
    {
        RECT wr; if (!GetWindowRect(hwnd, out wr)) return "GetWindowRect failed";
        int w=wr.Right-wr.Left, h=wr.Bottom-wr.Top;
        if (w<=0||h<=0) return "bad size";
        // The visible window sits inside an invisible DWM resize border; PrintWindow blackens
        // that border. Crop to the real visible bounds (DWMWA_EXTENDED_FRAME_BOUNDS = 9).
        int il=0, it=0, iw=w, ih=h; RECT fr;
        if (DwmGetWindowAttribute(hwnd, 9, out fr, Marshal.SizeOf(typeof(RECT)))==0) {
            il=fr.Left-wr.Left; it=fr.Top-wr.Top; iw=fr.Right-fr.Left; ih=fr.Bottom-fr.Top;
        }
        using (var full=new Bitmap(w,h,PixelFormat.Format32bppArgb)) {
            using (var g=Graphics.FromImage(full)) {
                IntPtr hdc=g.GetHdc(); bool ok=PrintWindow(hwnd,hdc,0x2); g.ReleaseHdc(hdc);
                if(!ok) return "PrintWindow false";
            }
            using (var outBmp=full.Clone(new Rectangle(il,it,iw,ih), PixelFormat.Format32bppArgb))
                outBmp.Save(outPath,ImageFormat.Png);
        }
        return "OK "+iw+"x"+ih;
    }
}
"@

[WinCap]::Dpi()
$proc = Start-Process $Exe -ArgumentList "--demo" -PassThru   # isolated read-only placeholder instance
Write-Host "Launched --demo PID $($proc.Id)"
$hwnd=[IntPtr]::Zero
for($i=0;$i -lt 50;$i++){ Start-Sleep -Milliseconds 200
    $p=Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
    if($p -and $p.MainWindowHandle -ne 0){ $hwnd=$p.MainWindowHandle; break } }
if($hwnd -eq [IntPtr]::Zero){ Write-Host "Window never appeared"; Stop-Process -Id $proc.Id -Force; exit 1 }
[WinCap]::ShowWindow($hwnd,9) | Out-Null
[WinCap]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 700
$cap=[WinCap]::Capture($hwnd,$Raw)
Stop-Process -Id $proc.Id -Force
Write-Host "Capture: $cap"
if(-not $cap.StartsWith("OK")){ exit 1 }

# ---------- compose ----------
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
public static class Compose
{
    static double[] H(PointF[] p, PointF[] q){
        double[,] A=new double[8,8]; double[] b=new double[8];
        for(int i=0;i<4;i++){ double x=p[i].X,y=p[i].Y,u=q[i].X,v=q[i].Y;
            A[2*i,0]=x;A[2*i,1]=y;A[2*i,2]=1;A[2*i,6]=-u*x;A[2*i,7]=-u*y;b[2*i]=u;
            A[2*i+1,3]=x;A[2*i+1,4]=y;A[2*i+1,5]=1;A[2*i+1,6]=-v*x;A[2*i+1,7]=-v*y;b[2*i+1]=v; }
        for(int c=0;c<8;c++){ int piv=c; for(int r=c+1;r<8;r++) if(Math.Abs(A[r,c])>Math.Abs(A[piv,c])) piv=r;
            for(int k=0;k<8;k++){double t=A[c,k];A[c,k]=A[piv,k];A[piv,k]=t;} double tb=b[c];b[c]=b[piv];b[piv]=tb;
            double d=A[c,c]; for(int r=0;r<8;r++){ if(r==c) continue; double f=A[r,c]/d; for(int k=0;k<8;k++)A[r,k]-=f*A[c,k]; b[r]-=f*b[c]; }
            for(int k=0;k<8;k++)A[c,k]/=d; b[c]/=d; }
        return new double[]{b[0],b[1],b[2],b[3],b[4],b[5],b[6],b[7],1};
    }
    static Color Sample(Bitmap src,double x,double y){
        int x0=(int)Math.Floor(x),y0=(int)Math.Floor(y);
        if(x0<0||y0<0||x0>=src.Width-1||y0>=src.Height-1) return Color.Transparent;
        double fx=x-x0,fy=y-y0;
        Color a=src.GetPixel(x0,y0),b=src.GetPixel(x0+1,y0),c=src.GetPixel(x0,y0+1),d=src.GetPixel(x0+1,y0+1);
        Func<int,int,int,int,int> lp=delegate(int p,int q,int r,int s){ return (int)Math.Round(p*(1-fx)*(1-fy)+q*fx*(1-fy)+r*(1-fx)*fy+s*fx*fy); };
        return Color.FromArgb(255,lp(a.R,b.R,c.R,d.R),lp(a.G,b.G,c.G,d.G),lp(a.B,b.B,c.B,d.B));
    }
    public static void Run(string raw,string outPath,double tilt,string title,string tag){
        var win=new Bitmap(raw); int w=win.Width,h=win.Height;
        int padX=190,padTop=140,padBottom=210; int W=w+padX*2,Hh=h+padTop+padBottom;
        var canvas=new Bitmap(W,Hh,PixelFormat.Format32bppArgb); var g=Graphics.FromImage(canvas);
        g.SmoothingMode=SmoothingMode.AntiAlias; g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.TextRenderingHint=TextRenderingHint.ClearTypeGridFit;
        using(var bg=new LinearGradientBrush(new Rectangle(0,0,W,Hh),Color.FromArgb(255,12,10,28),Color.FromArgb(255,22,16,46),55f)) g.FillRectangle(bg,0,0,W,Hh);
        int cx=W/2,cy=padTop+h/2;
        using(var path=new GraphicsPath()){ int gr=(int)(w*0.95); path.AddEllipse(cx-gr,cy-gr,gr*2,gr*2);
            var pg=new PathGradientBrush(path); pg.CenterPoint=new PointF(cx,cy); pg.CenterColor=Color.FromArgb(150,86,58,150);
            pg.SurroundColors=new[]{Color.FromArgb(0,86,58,150)}; g.FillPath(pg,path); }
        int ox=padX,oy=padTop;
        var dst=new PointF[]{ new PointF(ox,oy), new PointF(ox+w,oy+(float)(h*tilt*0.5)), new PointF(ox+w,oy+h-(float)(h*tilt*0.5)), new PointF(ox,oy+h) };
        var srcC=new PointF[]{ new PointF(0,0), new PointF(w,0), new PointF(w,h), new PointF(0,h) };
        var hm=H(dst,srcC);
        int minX=(int)Math.Floor(Math.Min(dst[0].X,dst[3].X)),maxX=(int)Math.Ceiling(Math.Max(dst[1].X,dst[2].X));
        int minY=(int)Math.Floor(Math.Min(dst[0].Y,dst[1].Y)),maxY=(int)Math.Ceiling(Math.Max(dst[2].Y,dst[3].Y));
        for(int pass=0;pass<3;pass++){ int off=10+pass*8; var shadow=new Bitmap(W,Hh,PixelFormat.Format32bppArgb);
            for(int dy=minY;dy<maxY;dy++) for(int dx=minX;dx<maxX;dx++){ double den=hm[6]*dx+hm[7]*dy+hm[8];
                double sx=(hm[0]*dx+hm[1]*dy+hm[2])/den,sy=(hm[3]*dx+hm[4]*dy+hm[5])/den;
                if(sx>=0&&sy>=0&&sx<w&&sy<h){ int tx=dx+off,ty=dy+off+6; if(tx>=0&&ty>=0&&tx<W&&ty<Hh) shadow.SetPixel(tx,ty,Color.FromArgb(28,0,0,0)); } }
            g.DrawImage(shadow,0,0); }
        var warped=new Bitmap(W,Hh,PixelFormat.Format32bppArgb);
        for(int dy=minY;dy<maxY;dy++) for(int dx=minX;dx<maxX;dx++){ double den=hm[6]*dx+hm[7]*dy+hm[8];
            double sx=(hm[0]*dx+hm[1]*dy+hm[2])/den,sy=(hm[3]*dx+hm[4]*dy+hm[5])/den;
            if(sx>=0&&sy>=0&&sx<w-1&&sy<h-1) warped.SetPixel(dx,dy,Sample(win,sx,sy)); }
        g.DrawImage(warped,0,0);
        int textY=padTop+h+58;
        var tf=new Font("Segoe UI Semibold",30,FontStyle.Regular,GraphicsUnit.Pixel); var tbr=new SolidBrush(Color.FromArgb(235,205,200,228));
        float spacing=8f,tw=0; foreach(char ch in title){ tw+=g.MeasureString(ch.ToString(),tf).Width+spacing; }
        float tx2=(W-tw)/2; foreach(char ch in title){ g.DrawString(ch.ToString(),tf,tbr,tx2,textY); tx2+=g.MeasureString(ch.ToString(),tf).Width+spacing; }
        var gf=new Font("Segoe UI",18,FontStyle.Regular,GraphicsUnit.Pixel); var gbr=new SolidBrush(Color.FromArgb(200,138,132,168));
        var gs=g.MeasureString(tag,gf); g.DrawString(tag,gf,gbr,(W-gs.Width)/2,textY+50);
        g.Flush(); canvas.Save(outPath,ImageFormat.Png);
        Console.WriteLine("Saved "+outPath+" "+W+"x"+Hh);
    }
}
"@

[Compose]::Run($Raw,$Out,$Tilt,$Title,$Tag)
Remove-Item $Raw -ErrorAction SilentlyContinue
$f=Get-Item $Out
Write-Host "screenshot.png  $([math]::Round($f.Length/1KB,1)) KB"
