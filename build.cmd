@echo off
set path=%path%;C:/Windows/Microsoft.NET/Framework/v4.0.30319;
set EnableNuGetPackageRestore=true

echo Building project...
msbuild src/Hydrospanner.sln /nologo /v:q /p:Configuration=Release /t:Clean
msbuild src/Hydrospanner.sln /nologo /v:q /p:Configuration=Release /clp:ErrorsOnly

echo Merging assemblies...
if exist "src\merged" rmdir /s /q "src\merged"
mkdir src\merged
bin\ilmerge\ILMerge.exe /keyfile:src\Hydrospanner.snk /internalize /wildcards /target:library ^
 /targetplatform:"v4,C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319" ^
 /out:"src\merged\Hydrospanner.dll" ^
 "src/Hydrospanner/bin/Release/Hydrospanner.dll" ^
 "src/Hydrospanner/bin/Release/Atomic.dll.dll" ^
 "src/Hydrospanner/bin/Release/Disruptor.dll" ^
 "src/Hydrospanner/bin/Release/Newtonsoft.Json.dll" ^
 "src/Hydrospanner/bin/Release/RabbitMQ.Client.dll" ^
 "src/Hydrospanner/bin/Release/System.IO.Abstractions.dll"
 
echo Creating NuGet packages...
for /r %%i in (src\packages\Hydrospanner*.nuspec) do src\.nuget\nuget.exe pack %%i -symbols

echo Done.