using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

// Creates a native, per-user Windows Installer package with embedded cabinet.
// No installer SDK, network service, custom elevated action or third-party compiler is required.
static class Program
{
    static uint db;
    static bool isolatedTest;
    static string ProductName=>isolatedTest?"LithoCreate Pro Installertest":"LithoCreate Pro";
    static readonly Dictionary<string,string[]> columnsByTable=[];
    static int Main(string[] args)
    {
        if(args.Length is < 2 or > 3||args.Length==3&&args[2]!="--isolated-test"){Console.Error.WriteLine("Aufruf: Packager <Veröffentlichungsordner> <Ausgabe.msi> [--isolated-test]");return 2;}
        isolatedTest=args.Length==3;
        try{Build(Path.GetFullPath(args[0]),Path.GetFullPath(args[1]));return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{if(db!=0)MsiCloseHandle(db);}
    }
    static void Build(string source,string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var files=Directory.GetFiles(source,"*",SearchOption.AllDirectories).Where(f=>!f.EndsWith(".pdb",StringComparison.OrdinalIgnoreCase)).Order().ToArray();
        if(!File.Exists(Path.Combine(source,"LithoCreate Pro.exe")))throw new FileNotFoundException("Die Anwendung fehlt.");
        var appVersion=FileVersionInfo.GetVersionInfo(Path.Combine(source,"LithoCreate Pro.dll"));
        string version=$"{appVersion.FileMajorPart}.{appVersion.FileMinorPart}.{appVersion.FileBuildPart}";
        string iconPath=Path.Combine(source,"Assets","LithoCreate.ico");
        const string iconName="LithoCreate.ico";
        bool hasIcon=File.Exists(iconPath);
        string staging=Path.Combine(Path.GetDirectoryName(output)!,"msi-staging-"+version);Directory.CreateDirectory(staging);
        string cab=Path.Combine(staging,"payload.cab"),ddf=Path.Combine(staging,"payload.ddf");
        // MakeCAB's directive parser does not read UTF-8 paths reliably. ASCII aliases also
        // work when the real file name or the enclosing user directory contains Unicode.
        var lines=new List<string>{".OPTION EXPLICIT",".Set CabinetNameTemplate=payload.cab",".Set DiskDirectoryTemplate=.",".Set CompressionType=MSZIP",".Set Cabinet=ON",".Set Compress=ON",".Set MaxDiskSize=0",".Set MaxCabinetSize=0",".Set FolderSizeThreshold=0"};
        for(int i=0;i<files.Length;i++)lines.Add($"content\\F{i} F{i}");
        File.WriteAllLines(ddf,lines,Encoding.ASCII);
        var start=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"makecab.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=staging};start.ArgumentList.Add("/F");start.ArgumentList.Add("payload.ddf");
        string manifest=string.Join("\n",files.Select(f=>$"{Path.GetRelativePath(source,f)}|{new FileInfo(f).Length}|{File.GetLastWriteTimeUtc(f).Ticks}"));
        string manifestPath=Path.Combine(staging,"inputs.txt");
        if(!File.Exists(cab)||!File.Exists(manifestPath)||File.ReadAllText(manifestPath)!=manifest)
        {
            string aliases=Path.Combine(staging,"content");Directory.CreateDirectory(aliases);
            for(int i=0;i<files.Length;i++)File.Copy(files[i],Path.Combine(aliases,"F"+i),true);
            using(var process=Process.Start(start)!){string log=process.StandardOutput.ReadToEnd()+process.StandardError.ReadToEnd();process.WaitForExit();File.WriteAllText(Path.Combine(staging,"cabinet.log"),log);if(process.ExitCode!=0)throw new Exception("Cabinet-Erstellung fehlgeschlagen: "+log);}
            File.WriteAllText(manifestPath,manifest);
        }
        if(File.Exists(output))File.Delete(output);
        Check(MsiOpenDatabase(output,(IntPtr)3,out db));
        File.WriteAllText(Path.Combine(staging,"_ForceCodepage.idt"),"\r\n\r\n1252\t_ForceCodepage\r\n",Encoding.ASCII);Check(MsiDatabaseImport(db,staging,"_ForceCodepage.idt"));
        Table("Property","`Property` CHAR(72) NOT NULL, `Value` CHAR(0) LOCALIZABLE","`Property`");
        Table("Directory","`Directory` CHAR(72) NOT NULL, `Directory_Parent` CHAR(72), `DefaultDir` CHAR(255) NOT NULL LOCALIZABLE","`Directory`");
        Table("Component","`Component` CHAR(72) NOT NULL, `ComponentId` CHAR(38), `Directory_` CHAR(72) NOT NULL, `Attributes` SHORT NOT NULL, `Condition` CHAR(255), `KeyPath` CHAR(72)","`Component`");
        Table("Feature","`Feature` CHAR(38) NOT NULL, `Feature_Parent` CHAR(38), `Title` CHAR(64) LOCALIZABLE, `Description` CHAR(255) LOCALIZABLE, `Display` SHORT, `Level` SHORT NOT NULL, `Directory_` CHAR(72), `Attributes` SHORT NOT NULL","`Feature`");
        Table("FeatureComponents","`Feature_` CHAR(38) NOT NULL, `Component_` CHAR(72) NOT NULL","`Feature_`, `Component_`");
        Table("File","`File` CHAR(72) NOT NULL, `Component_` CHAR(72) NOT NULL, `FileName` CHAR(255) NOT NULL LOCALIZABLE, `FileSize` LONG NOT NULL, `Version` CHAR(72), `Language` CHAR(20), `Attributes` SHORT, `Sequence` LONG NOT NULL","`File`");
        Table("Media","`DiskId` SHORT NOT NULL, `LastSequence` LONG NOT NULL, `DiskPrompt` CHAR(64) LOCALIZABLE, `Cabinet` CHAR(255), `VolumeLabel` CHAR(32), `Source` CHAR(72)","`DiskId`");
        Table("Shortcut","`Shortcut` CHAR(72) NOT NULL, `Directory_` CHAR(72) NOT NULL, `Name` CHAR(128) NOT NULL LOCALIZABLE, `Component_` CHAR(72) NOT NULL, `Target` CHAR(255) NOT NULL, `Arguments` CHAR(255), `Description` CHAR(255) LOCALIZABLE, `Hotkey` SHORT, `Icon_` CHAR(72), `IconIndex` SHORT, `ShowCmd` SHORT, `WkDir` CHAR(72)","`Shortcut`");
        Table("Registry","`Registry` CHAR(72) NOT NULL, `Root` SHORT NOT NULL, `Key` CHAR(255) NOT NULL LOCALIZABLE, `Name` CHAR(255) LOCALIZABLE, `Value` CHAR(0) LOCALIZABLE, `Component_` CHAR(72) NOT NULL","`Registry`");
        Table("RemoveFile","`FileKey` CHAR(72) NOT NULL, `Component_` CHAR(72) NOT NULL, `FileName` CHAR(255) LOCALIZABLE, `DirProperty` CHAR(72) NOT NULL, `InstallMode` SHORT NOT NULL","`FileKey`");
        Table("Upgrade","`UpgradeCode` CHAR(38) NOT NULL, `VersionMin` CHAR(20), `VersionMax` CHAR(20), `Language` CHAR(255), `Attributes` LONG NOT NULL, `Remove` CHAR(255), `ActionProperty` CHAR(72) NOT NULL","`UpgradeCode`, `VersionMin`, `VersionMax`, `Language`, `Attributes`");
        Table("LaunchCondition","`Condition` CHAR(255) NOT NULL, `Description` CHAR(255) NOT NULL LOCALIZABLE","`Condition`");
        foreach(string table in new[]{"InstallExecuteSequence","InstallUISequence","AdminExecuteSequence","AdminUISequence"})Table(table,"`Action` CHAR(72) NOT NULL, `Condition` CHAR(255), `Sequence` SHORT","`Action`");
        string upgrade=isolatedTest?StableGuid("isolated-upgrade"):"{90D2DB61-B471-4438-8DFA-152529F429A0}";
        foreach(var property in new Dictionary<string,string>{["ProductCode"]=!isolatedTest&&version=="1.0.0"?"{DA55B6C0-27CF-4B82-AD41-D02CE04149B8}":StableGuid("product/"+version),["UpgradeCode"]=upgrade,["ProductName"]=ProductName,["ProductVersion"]=version,["ProductLanguage"]="1031",["Manufacturer"]="LithoCreate Pro",["ALLUSERS"]="2",["MSIINSTALLPERUSER"]="1",["INSTALLLEVEL"]="1",["ARPNOMODIFY"]="1",["ARPNOREPAIR"]="1",["ARPHELPLINK"]="",["SecureCustomProperties"]="OLDPRODUCTS;NEWERPRODUCTS;DESKTOP",["DefaultUIFont"]="Normal"})if(property.Value!="")Row("Property",property.Key,property.Value);
        if(hasIcon)
        {
            foreach(string table in new[]{"Icon","Binary"})
            {
                Table(table,"`Name` CHAR(72) NOT NULL, `Data` OBJECT NOT NULL","`Name`");
                AddStream(table,iconName,iconPath);
            }
            Row("Property","ARPPRODUCTICON",iconName);
        }
        Row("Directory","TARGETDIR",null,"SourceDir");Row("Directory","LocalAppDataFolder","TARGETDIR",".");Row("Directory","ProgramsDir","LocalAppDataFolder","Programs");Row("Directory","INSTALLDIR","ProgramsDir",ProductName);Row("Directory","ProgramMenuFolder","TARGETDIR",".");Row("Directory","MenuDir","ProgramMenuFolder",ProductName);Row("Directory","DesktopFolder","TARGETDIR",".");
        Row("Feature","Core",null,"LithoCreate Pro","Anwendung und integrierte .NET-Laufzeit",1,1,"INSTALLDIR",0);
        var dirs=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){[source]="INSTALLDIR"};
        string Dir(string path)
        {
            if(dirs.TryGetValue(path,out string? id))return id;
            string parent=Dir(Path.GetDirectoryName(path)!);id="D"+dirs.Count;dirs[path]=id;Row("Directory",id,parent,Path.GetFileName(path));return id;
        }
        string? appComponent=null,appFile=null;
        for(int i=0;i<files.Length;i++)
        {
            string file="F"+i,component="C"+i,relative=Path.GetRelativePath(source,files[i]);
            string guid=StableGuid(relative);
            Row("Component",component,guid,Dir(Path.GetDirectoryName(files[i])!),256,null,file);Row("FeatureComponents","Core",component);
            string? fileVersion=null;var info=FileVersionInfo.GetVersionInfo(files[i]);if(info.FileMajorPart>0)fileVersion=$"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}.{info.FilePrivatePart}";
            Row("File",file,component,Path.GetFileName(files[i]),checked((int)new FileInfo(files[i]).Length),fileVersion,null,512,i+1);
            if(relative=="LithoCreate Pro.exe"){appComponent=component;appFile=file;}
        }
        if(appComponent==null)throw new Exception("EXE fehlt im Paket.");
        Row("Shortcut","StartMenu","MenuDir",ProductName,appComponent,"[#"+appFile+"]",null,"Offline-Lithophaniestudio",null,null,null,1,"INSTALLDIR");
        Row("Component","DesktopShortcut",StableGuid("desktop-shortcut"),"DesktopFolder",260,"DESKTOP=1","DesktopReg");Row("FeatureComponents","Core","DesktopShortcut");
        Row("Registry","DesktopReg",1,"Software\\"+ProductName,"DesktopShortcut","#1","DesktopShortcut");
        Row("Shortcut","Desktop","DesktopFolder",ProductName,"DesktopShortcut","[#"+appFile+"]",null,"Offline-Lithophaniestudio",null,null,null,1,"INSTALLDIR");
        Row("RemoveFile","MenuFolder",appComponent,null,"MenuDir",2);
        Row("Media",1,files.Length,null,"#payload.cab",null,null);
        Row("Upgrade",upgrade,null,version,null,256,null,"OLDPRODUCTS");Row("Upgrade",upgrade,version,null,null,2,null,"NEWERPRODUCTS");
        Row("LaunchCondition","VersionNT64","LithoCreate Pro benötigt Windows 10 oder 11 (64 Bit).");Row("LaunchCondition","NOT NEWERPRODUCTS","Eine neuere Version von LithoCreate Pro ist bereits installiert.");
        var actions=new (string Name,string? Condition,int Sequence)[]{("FindRelatedProducts",null,25),("LaunchConditions",null,100),("ValidateProductID",null,700),("CostInitialize",null,800),("FileCost",null,900),("CostFinalize",null,1000),("InstallValidate",null,1400),("InstallInitialize",null,1500),("RemoveExistingProducts","OLDPRODUCTS",1510),("ProcessComponents",null,1600),("UnpublishFeatures",null,1800),("RemoveShortcuts",null,3200),("RemoveFiles",null,3500),("RemoveFolders",null,3600),("CreateFolders",null,3700),("InstallFiles",null,4000),("CreateShortcuts",null,4500),("RemoveRegistryValues",null,2600),("WriteRegistryValues",null,5000),("RegisterUser",null,6000),("RegisterProduct",null,6100),("PublishFeatures",null,6300),("PublishProduct",null,6400),("InstallFinalize",null,6600)};
        foreach(var a in actions)Row("InstallExecuteSequence",a.Name,a.Condition,a.Sequence);
        foreach(var a in actions.Where(a=>a.Sequence<=1000))Row("InstallUISequence",a.Name,a.Condition,a.Sequence);
        Row("InstallUISequence","WelcomeDlg","NOT Installed",750);Row("InstallUISequence","ExecuteAction",null,1300);Row("InstallUISequence","FinishDlg",null,-1);
        foreach(var action in actions.Where(a=>a.Name is "CostInitialize" or "FileCost" or "CostFinalize" or "InstallValidate" or "InstallInitialize" or "InstallFiles" or "InstallFinalize"))Row("AdminExecuteSequence",action.Name,null,action.Sequence);
        Row("AdminExecuteSequence","InstallAdminPackage",null,3900);
        foreach(var action in actions.Where(a=>a.Name is "CostInitialize" or "FileCost" or "CostFinalize"))Row("AdminUISequence",action.Name,null,action.Sequence);
        Row("AdminUISequence","ExecuteAction",null,1300);
        UserInterface(version,hasIcon?iconName:null);
        AddStream("_Streams","payload.cab",cab);
        Check(MsiGetSummaryInformation(db,null,10,out uint summary));
        Set(2,30,"LithoCreate Pro · Installation");Set(3,30,"Offline-Lithophanien-Generator");Set(4,30,"LithoCreate Pro");Set(7,30,"x64;1031");Set(9,30,Guid.NewGuid().ToString("B").ToUpperInvariant());Set(14,3,null,200);Set(15,3,null,2);Set(19,3,null,2);
        Check(MsiSummaryInfoPersist(summary));MsiCloseHandle(summary);Check(MsiDatabaseCommit(db));MsiCloseHandle(db);db=0;
        Console.WriteLine($"MSI erstellt: {output} · {files.Length} Dateien · {new FileInfo(output).Length/1024/1024} MB");
        void Set(uint property,uint type,string? value,int number=0)=>Check(MsiSummaryInfoSetProperty(summary,property,type,number,IntPtr.Zero,value));
    }
    static void UserInterface(string version,string? iconName)
    {
        Table("Dialog","`Dialog` CHAR(72) NOT NULL, `HCentering` SHORT NOT NULL, `VCentering` SHORT NOT NULL, `Width` SHORT NOT NULL, `Height` SHORT NOT NULL, `Attributes` LONG, `Title` CHAR(128) LOCALIZABLE, `Control_First` CHAR(50), `Control_Default` CHAR(50), `Control_Cancel` CHAR(50)","`Dialog`");
        Table("Control","`Dialog_` CHAR(72) NOT NULL, `Control` CHAR(50) NOT NULL, `Type` CHAR(20) NOT NULL, `X` SHORT NOT NULL, `Y` SHORT NOT NULL, `Width` SHORT NOT NULL, `Height` SHORT NOT NULL, `Attributes` LONG, `Property` CHAR(72), `Text` CHAR(0) LOCALIZABLE, `Control_Next` CHAR(50), `Help` CHAR(50) LOCALIZABLE","`Dialog_`, `Control`");
        Table("ControlEvent","`Dialog_` CHAR(72) NOT NULL, `Control_` CHAR(50) NOT NULL, `Event` CHAR(50) NOT NULL, `Argument` CHAR(255) NOT NULL, `Condition` CHAR(255), `Ordering` SHORT","`Dialog_`, `Control_`, `Event`, `Argument`, `Condition`");
        Table("CheckBox","`Property` CHAR(72) NOT NULL, `Value` CHAR(64)","`Property`");
        Table("TextStyle","`TextStyle` CHAR(72) NOT NULL, `FaceName` CHAR(32) NOT NULL, `Size` SHORT NOT NULL, `Color` LONG, `StyleBits` SHORT","`TextStyle`");
        Row("TextStyle","Normal","Segoe UI",9,0,0);Row("TextStyle","Title","Segoe UI",18,0,1);Row("CheckBox","DESKTOP","1");
        Row("Dialog","WelcomeDlg",50,50,390,245,3,"LithoCreate Pro · Installation","Desktop","Install","Cancel");
        if(iconName is not null)
        {
            // Visible, fixed size, 32 px image. Decorative controls stay out of the tab order.
            Row("Control","WelcomeDlg","AppIcon","Icon",22,15,28,28,0x00500001,null,iconName,null,null);
        }
        int titleX=iconName is null?22:56;
        C("WelcomeDlg","Title","Text",titleX,18,372-titleX,35,null,@"{\Title}LithoCreate Pro");
        C("WelcomeDlg","Description","Text",22,67,345,85,null,$"Dein Offline-Studio für 3D-druckbare Lithophanien.\r\n\r\nVersion {version} · Windows x64\r\nInstallation für den aktuellen Benutzer.\r\nDie .NET-Laufzeit ist enthalten. Kein Internet erforderlich.");
        C("WelcomeDlg","Desktop","CheckBox",22,163,340,20,"DESKTOP","Desktopverknüpfung erstellen","Install");
        C("WelcomeDlg","Install","PushButton",200,205,80,24,null,"Installieren","Cancel");C("WelcomeDlg","Cancel","PushButton",288,205,80,24,null,"Abbrechen","Desktop");
        Row("ControlEvent","WelcomeDlg","Install","EndDialog","Return","1",1);Row("ControlEvent","WelcomeDlg","Cancel","EndDialog","Exit","1",1);
        Row("Dialog","FinishDlg",50,50,390,210,3,"LithoCreate Pro","Close","Close","Close");
        C("FinishDlg","Title","Text",22,20,345,35,null,@"{\Title}Vorgang abgeschlossen");C("FinishDlg","Description","Text",22,75,345,70,null,"Windows Installer hat den Vorgang erfolgreich abgeschlossen.\r\n\r\nGespeicherte Benutzerprojekte bleiben erhalten.");C("FinishDlg","Close","PushButton",280,165,85,24,null,"Schließen","Close");Row("ControlEvent","FinishDlg","Close","EndDialog","Return","1",1);
        void C(string dialog,string name,string type,int x,int y,int width,int height,string? property,string text,string? next=null)=>Row("Control",dialog,name,type,x,y,width,height,3,property,text,next,null);
    }
    static string StableGuid(string value)=>new Guid(SHA256.HashData(Encoding.UTF8.GetBytes("LithoCreate Pro/"+(isolatedTest?"isolated-test/":"")+value.ToLowerInvariant()))[..16]).ToString("B").ToUpperInvariant();
    static void Table(string table,string columns,string key)
    {
        columnsByTable[table]=System.Text.RegularExpressions.Regex.Matches(columns,@"(?:^|,)\s*`([^`]+)`").Select(m=>m.Groups[1].Value).ToArray();
        Sql($"CREATE TABLE `{table}` ({columns} PRIMARY KEY {key})");
    }
    static void Row(string table,params object?[] values)
    {
        Check(MsiDatabaseOpenView(db,$"INSERT INTO `{table}` ({string.Join(",",columnsByTable[table].Select(c=>"`"+c+"`"))}) VALUES ({string.Join(",",values.Select(_=>"?"))})",out uint view));uint record=MsiCreateRecord((uint)values.Length);
        try{for(int i=0;i<values.Length;i++)if(values[i] is int number)Check(MsiRecordSetInteger(record,(uint)i+1,number));else if(values[i]!=null)Check(MsiRecordSetString(record,(uint)i+1,values[i]!.ToString()));Check(MsiViewExecute(view,record));}finally{MsiCloseHandle(record);MsiCloseHandle(view);}
    }
    static void Sql(string sql){Check(MsiDatabaseOpenView(db,sql,out uint view));try{Check(MsiViewExecute(view,0));}finally{MsiCloseHandle(view);}}
    static void AddStream(string table,string name,string path)
    {
        Check(MsiDatabaseOpenView(db,$"INSERT INTO `{table}` (`Name`, `Data`) VALUES (?, ?)",out uint view));
        uint record=MsiCreateRecord(2);
        try
        {
            Check(MsiRecordSetString(record,1,name));
            Check(MsiRecordSetStream(record,2,path));
            Check(MsiViewExecute(view,record));
        }
        finally
        {
            MsiCloseHandle(record);
            MsiCloseHandle(view);
        }
    }
    static void Check(uint code){if(code!=0)throw new InvalidOperationException("Windows Installer API Fehler "+code);}
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiOpenDatabaseW")]static extern uint MsiOpenDatabase(string path,IntPtr mode,out uint database);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiDatabaseOpenViewW")]static extern uint MsiDatabaseOpenView(uint database,string query,out uint view);
    [DllImport("msi.dll")]static extern uint MsiViewExecute(uint view,uint record);
    [DllImport("msi.dll")]static extern uint MsiCreateRecord(uint fields);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiRecordSetStringW")]static extern uint MsiRecordSetString(uint record,uint field,string? value);
    [DllImport("msi.dll")]static extern uint MsiRecordSetInteger(uint record,uint field,int value);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiRecordSetStreamW")]static extern uint MsiRecordSetStream(uint record,uint field,string value);
    [DllImport("msi.dll")]static extern uint MsiDatabaseCommit(uint database);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiDatabaseImportW")]static extern uint MsiDatabaseImport(uint database,string folder,string file);
    [DllImport("msi.dll")]static extern uint MsiCloseHandle(uint handle);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiGetSummaryInformationW")]static extern uint MsiGetSummaryInformation(uint database,string? path,uint count,out uint summary);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiSummaryInfoSetPropertyW")]static extern uint MsiSummaryInfoSetProperty(uint summary,uint property,uint type,int number,IntPtr filetime,string? value);
    [DllImport("msi.dll")]static extern uint MsiSummaryInfoPersist(uint summary);
}
