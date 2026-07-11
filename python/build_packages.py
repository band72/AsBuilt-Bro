import os
import sys
import subprocess
import shutil

def check_installed(pkg_name):
    try:
        __import__(pkg_name)
        return True
    except ImportError:
        return False

def run_cmd(cmd, cwd=None):
    print(f"Executing: {' '.join(cmd)}")
    res = subprocess.run(cmd, cwd=cwd)
    if res.returncode != 0:
        print(f"Error executing command: {' '.join(cmd)}")
        return False
    return True

def build_pyinstaller():
    print("=== Building standalone executable with PyInstaller ===")
    if not shutil.which("pyinstaller"):
        print("PyInstaller not found. Installing it via pip...")
        if not run_cmd([sys.executable, "-m", "pip", "install", "pyinstaller"]):
            print("Failed to install PyInstaller. Please run: pip install pyinstaller")
            return False

    cmd = [
        "pyinstaller",
        "--noconsole",
        "--onefile",
        "--name=AsBuiltBro",
        "app.py"
    ]
    
    # Check for icon file
    icon_path = "../rcs_cogo_icon.ico"
    if os.path.exists(icon_path):
        cmd.append(f"--icon={icon_path}")
        
    if run_cmd(cmd):
        print("\n[SUCCESS] PyInstaller build complete!")
        print("Standalone executable is located in: python/dist/AsBuiltBro (or AsBuiltBro.exe)")
        return True
    return False

def build_briefcase(platform):
    print(f"=== Building {platform} package with BeeWare Briefcase ===")
    if not shutil.which("briefcase"):
        print("Briefcase not found. Installing it via pip...")
        if not run_cmd([sys.executable, "-m", "pip", "install", "briefcase"]):
            print("Failed to install Briefcase. Please run: pip install briefcase")
            return False

    # Run briefcase commands
    print(f"Creating Briefcase scaffold for {platform}...")
    if not run_cmd(["briefcase", "create", platform]):
        return False
        
    print(f"Building Briefcase app for {platform}...")
    if not run_cmd(["briefcase", "build", platform]):
        return False
        
    print(f"Packaging Briefcase app for {platform}...")
    if not run_cmd(["briefcase", "package", platform]):
        return False
        
    print(f"\n[SUCCESS] Briefcase packaging complete for {platform}!")
    return True

def main():
    print("=" * 60)
    print("AsBuilt-Bro Python Cross-Platform Packaging Assistant")
    print("=" * 60)
    print("Supported Platforms:")
    print("  1. Windows / Desktop Standalone (via PyInstaller)")
    print("  2. Mobile / Tablet (Android APK via Briefcase)")
    print("  3. Mobile / Tablet (iOS via Briefcase)")
    print("  4. All of the above")
    print("-" * 60)
    
    if len(sys.argv) > 1:
        choice = sys.argv[1].lower()
    else:
        print("Usage: python build_packages.py [windows|android|ios|all]")
        sys.exit(0)

    success = True
    if choice in ("windows", "all"):
        success = success and build_pyinstaller()
    if choice in ("android", "all"):
        success = success and build_briefcase("android")
    if choice in ("ios", "all"):
        success = success and build_briefcase("iOS")
        
    if success:
        print("\nAll requested packages compiled successfully!")
    else:
        print("\nSome packaging builds encountered errors. Check logs above.")

if __name__ == "__main__":
    main()
