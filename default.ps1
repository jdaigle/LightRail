Framework "4.6.1"

properties {
    $baseDir  = resolve-path .
    $buildDir = "$baseDir\build"
    $artifactsDir = "$baseDir\artifacts"
    $toolsDir = "$baseDir\tools"
    $nugetExePath = "$toolsDir\nuget\nuget.exe"
    $slnFiles = "$baseDir\src\LightRail.sln"
    $testAsms = "LightRail.Amqp.UnitTests.dll",
                "LightRail.ServiceBus.UnitTests.dll"
    $nunitexec = "tools\NUnit\nunit-console.exe"
}

if ($env:APPVEYOR -eq $true) {
    $nunitexec = "C:\Tools\NUnit\bin\nunit-console.exe"
}

include $PSScriptRoot\tools\psake\buildutils.ps1

task default -depends Build

task Clean {
    if (Test-Path $buildDir) {
        Delete-Directory $buildDir
    }
    if (Test-Path $artifactsDir) {
        Delete-Directory $artifactsDir
    }
    foreach ($slnFile in $slnFiles) {
        exec { msbuild $slnFile /v:m /nologo /p:Configuration=Debug /m /target:Clean }
        exec { msbuild $slnFile /v:m /nologo /p:Configuration=Release /m /target:Clean }
    }
}

task NuGetRestore -Description "Restores NuGet packages for solutions" {
    foreach ($slnFile in $slnFiles) {
        exec { &$nugetExePath restore $slnFile -verbosity detailed }
    }
}

task Init -depends NuGetRestore, Clean {
    echo "Creating build directory at the follwing path $buildDir"
    Create-Directory($buildDir);
    Create-Directory($artifactsDir);

    $currentDirectory = Resolve-Path .

    echo "Current Directory: $currentDirectory"
}

task Compile -depends Init {
    foreach ($slnFile in $slnFiles) {
        exec { msbuild $slnFile /v:n /nologo /p:Configuration=Release /m /p:AllowedReferenceRelatedFileExtensions=none /p:OutDir="$buildDir\" }
    }
}

task Test -depends Compile {
    foreach ($asm in $testAsms) {
        echo "Path To NUnit: $nunitexec"
        exec { &$nunitexec $buildDir\$asm /framework=4.6.1 }
    }
}

task Build -depends Compile, Test {

}