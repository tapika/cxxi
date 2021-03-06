-- This module checks for the all the project dependencies.

action = _ACTION or ""

depsdir = path.getabsolute("../deps");
srcdir = path.getabsolute("../src");
incdir = path.getabsolute("../inc");
bindir = path.getabsolute("../bin");
examplesdir = path.getabsolute("../examples");
testsdir = path.getabsolute("../tests");

builddir = path.getabsolute("./" .. action);
libdir = path.join(builddir, "lib");

common_flags = { "Unicode", "Symbols" }
msvc_buildflags = { } -- "/wd4190", "/wd4996", "/wd4530"
gcc_buildflags = { "-std=gnu++11" }

msvc_cpp_defines = { }

function SetupNativeProject()
  location (path.join(builddir, "projects"))

  c = configuration "Debug"
    defines { "DEBUG" }
    targetsuffix "_d"
    
  configuration "Release"
    defines { "NDEBUG" }
    
  -- Compiler-specific options
  
  configuration "vs*"
    buildoptions { msvc_buildflags }
    defines { msvc_cpp_defines }
    
  configuration "gcc"
    buildoptions { gcc_buildflags }
  
  -- OS-specific options
  
  configuration "Windows"
    defines { "WIN32", "_WINDOWS" }
  
  configuration(c)
end

function IncludeDir(dir)
  local deps = os.matchdirs(dir .. "/*")
  
  for i,dep in ipairs(deps) do
    local fp = path.join(dep, "premake4.lua")
    fp = path.join(os.getcwd(), fp)
    
    if os.isfile(fp) then
      print(string.format(" including %s", dep))
      include(dep)
    end
  end
end