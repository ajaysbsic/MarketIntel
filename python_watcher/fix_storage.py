# fix_storage.py
"""
Fix file storage by copying PDFs to API storage folder
"""

import os
import shutil
from pathlib import Path

print("=" * 70)
print("Fixing File Storage")
print("=" * 70)

# Paths
watcher_downloads = Path("downloads")
api_storage = Path("../Alfanar.MarketIntel.Api/storage/reports")

# Create API storage folder
api_storage.mkdir(parents=True, exist_ok=True)
print(f"\n? Created API storage folder: {api_storage.absolute()}")

# Copy existing PDFs
if watcher_downloads.exists():
    pdf_files = list(watcher_downloads.glob("*.pdf"))
    
    if pdf_files:
        print(f"\n?? Found {len(pdf_files)} PDFs to copy...")
        
        for pdf_file in pdf_files:
            dest_file = api_storage / pdf_file.name
            if not dest_file.exists():
                shutil.copy2(pdf_file, dest_file)
                print(f"  ? Copied: {pdf_file.name}")
            else:
                print(f"  ? Skipped (already exists): {pdf_file.name}")
        
        print(f"\n? Copied {len(pdf_files)} PDFs to API storage")
    else:
        print("\n??  No PDFs found in downloads folder")
else:
    print("\n??  Downloads folder doesn't exist yet")

print("\n" + "=" * 70)
print("? Storage fix complete!")
print("=" * 70)
print("\nPDFs are now accessible at:")
print(f"  {api_storage.absolute()}")
print("\nRestart the API to refresh file access.")
