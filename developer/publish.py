import subprocess
import os
import sys

def run(cmd):
    print("> " + " ".join(cmd))
    result = subprocess.run(cmd)
    if result.returncode != 0:
        sys.exit(result.returncode)

print("========================================")
print("  TOOLPROFILE PUBLISH - FIXED")
print("========================================\n")

print("Cleaning...")
run(["dotnet", "clean"])

print("Publishing...")
run([
    "dotnet", "publish",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "--output", "./Publish"
])

print("\n========================================")
print("  DONE!")
print("========================================\n")
print("Output:", os.path.abspath("./Publish"))

input("Press Enter to exit")
