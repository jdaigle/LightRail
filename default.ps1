Framework "4.5.1x64"

properties {
}

$baseDir  = resolve-path .
$buildDir = "$baseDir\build"
$toolsDir = "$baseDir\tools"
$coreSlns = "$baseDir\LightRail.sln"
$testAsms = "LightRail.Client.UnitTests.dll"
$nunitexec = "tools\NUnit\nunit-console.exe"

if ($env:APPVEYOR -eq $true) {
    $nunitexec = "C:\Tools\NUnit\bin\nunit-console.exe"
}

include $toolsDir\psake\buildutils.ps1

task default -depends Build

task Clean {
    if (Test-Path $buildDir) {
        Delete-Directory $buildDir
    }
    foreach ($slnFile in $coreSlns) {
        exec { msbuild $slnFile /v:minimal /nologo /p:Configuration=Debug /m /target:Clean }
        exec { msbuild $slnFile /v:minimal /nologo /p:Configuration=Release /m /target:Clean }
    }
}

task Init -depends Clean {
    echo "Creating build directory at the follwing path $buildDir"
    if (Test-Path $buildDir) {
        Delete-Directory $buildDir;
    }
    Create-Directory($buildDir);

    $currentDirectory = Resolve-Path .

    echo "Current Directory: $currentDirectory"
}

task Compile -depends Init {
    foreach ($slnFile in $coreSlns) {
        exec { msbuild $slnFile /v:minimal /nologo /p:Configuration=Release /m /p:AllowedReferenceRelatedFileExtensions=none /p:OutDir="$buildDir\" }
    }
}

task Test -depends Compile {
    foreach ($asm in $testAsms) {
        echo "Path To NUnit: $nunitexec"
        exec { &$nunitexec $buildDir\$asm /framework=4.0 }
    }
}

task Build -depends Compile, Test {

}