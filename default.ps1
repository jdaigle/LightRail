Framework "4.5.1x64"

properties {
}

$baseDir  = resolve-path .
$buildDir = "$baseDir\build"
$toolsDir = "$baseDir\tools"
$coreSlns = "$baseDir\LightRail.sln"

include $toolsDir\psake\buildutils.ps1

task default -depends Compile

task Clean {
  if (Test-Path $buildDir) {
    Delete-Directory $buildDir	
  }
  foreach ($slnFile in $coreSlns) {
      msbuild $slnFile /p:Configuration=Debug /m /target:Clean
      msbuild $slnFile /p:Configuration=Release /m /target:Clean
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
        msbuild $slnFile /p:Configuration=Release /m /p:OutDir="$buildDir\"
    }
}