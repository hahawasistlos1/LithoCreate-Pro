if(-not ('LithoCreateInstallerApi' -as [type])){
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class LithoCreateInstallerApi
{
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiOpenDatabaseW")] static extern uint Open(string path,IntPtr mode,out uint db);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiDatabaseOpenViewW")] static extern uint View(uint db,string sql,out uint view);
    [DllImport("msi.dll",EntryPoint="MsiViewExecute")] static extern uint Execute(uint view,uint record);
    [DllImport("msi.dll",EntryPoint="MsiViewFetch")] static extern uint Fetch(uint view,out uint record);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiRecordGetStringW")] static extern uint Get(uint record,uint field,StringBuilder value,ref uint count);
    [DllImport("msi.dll",EntryPoint="MsiCloseHandle")] static extern uint Close(uint handle);
    [DllImport("msi.dll",CharSet=CharSet.Unicode,EntryPoint="MsiQueryProductStateW")] public static extern int ProductState(string product);
    public static string Property(string path,string property)
    {
        if(property!="ProductCode"&&property!="ProductVersion"&&property!="UpgradeCode"&&property!="ProductName")throw new ArgumentException("Unzulässige MSI-Eigenschaft.");
        uint db=0,view=0,record=0;
        try{Check(Open(path,IntPtr.Zero,out db));Check(View(db,"SELECT `Value` FROM `Property` WHERE `Property`='"+property+"'",out view));Check(Execute(view,0));Check(Fetch(view,out record));uint count=1024;var value=new StringBuilder((int)count);Check(Get(record,1,value,ref count));return value.ToString();}
        finally{if(record!=0)Close(record);if(view!=0)Close(view);if(db!=0)Close(db);}
    }
    public static string[] Components(string path)
    {
        uint db=0,view=0,record=0;var values=new System.Collections.Generic.List<string>();
        try{Check(Open(path,IntPtr.Zero,out db));Check(View(db,"SELECT `ComponentId` FROM `Component`",out view));Check(Execute(view,0));uint code;while((code=Fetch(view,out record))==0){uint count=128;var text=new StringBuilder((int)count);Check(Get(record,1,text,ref count));values.Add(text.ToString());Close(record);record=0;}if(code!=259)Check(code);return values.ToArray();}
        finally{if(record!=0)Close(record);if(view!=0)Close(view);if(db!=0)Close(db);}
    }
    static void Check(uint code){if(code!=0)throw new InvalidOperationException("MSI-API: "+code);}
}
'@
}
