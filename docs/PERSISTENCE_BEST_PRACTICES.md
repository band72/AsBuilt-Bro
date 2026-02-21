# Data Persistence Best Practices

For a modern desktop engineering application like RCS Cogo Enterprise, data persistence strategy is critical for reliability, performance, and user trust.

## 1. Embedded Relational Database (Recommended)
This is the **Gold Standard** for applications with complex relationships (e.g., Pipe Networks where Runs connect to Structures which connect to Points).

### **SQLite**
*   **Pros**: 
    *   Industry standard, rock-solid reliability.
    *   Supports complex SQL queries and transactions (ACID compliant).
    *   Single file `.db` format, easy to backup/transfer.
    *   Excellent tooling (EF Core support).
*   **Cons**: Requires schema migrations when model changes.
*   **Best For**: Complex relational data, data integrity enforcement.

### **LiteDB** (NoSQL)
*   **Pros**:
    *   **Native .NET**: Written in C#, extremely easy to integrate.
    *   **NoSQL**: Stores objects directly as documents (BSON). Perfect for your hierarchical `Project -> PipeRuns`, `Project -> Points`.
    *   **Schema-less**: Flexible schema ("Lite"), easier to evolve during rapid development.
    *   **Single File**: Like SQLite, but often simpler API for .NET developers.
*   **Cons**: Less tooling than SQLite; complex reporting queries can be harder than SQL.
*   **Best For**: Rapid development, hierarchical object graphs, .NET-centric teams.

## 2. Document Serialization (Current Implementation)
Currently, we are using **JSON Serialization** (`System.Text.Json`).
*   **Pros**: Human readable, zero dependencies, simplest implementation.
*   **Cons**: 
    *   Performance issues with large datasets (must load/save *entire* file at once).
    *   Risk of corruption if crash occurs during write (unless "Atomic Save" is implemented).
    *   No query capability (must load all into RAM to find one point).
*   **Recommendation**: Suitable for "File -> Save" workflows with small-to-medium datasets (< 50k points).

## 3. Best Practices Checklist
Regardless of the engine chosen, follow these practices:

1.  **Atomic Saves**: Never write directly to the target file. 
    *   Write to `Project.tmp`.
    *   Flush to disk.
    *   Rename `Project.tmp` to `Project.json` (replacing original).
    *   *This prevents 0-byte files if power fails during save.*
    
2.  **Auto-Backup**:
    *   Create a `.bak` copy before overwriting.
    *   Implement an auto-save timer (e.g., every 5 mins) to a separate auto-save file.

3.  **Versioning**:
    *   Always include a `"Version": "1.0"` field in your file header.
    *   This allows you to migrate data structure changes in future updates.

## Recommendation for RCS Cogo
Given the current state (rapid iteration, hierarchical data):
1.  **Immediate Term**: Stick with **JSON** but implement **Atomic Saves** to prevent corruption.
2.  **Mid Term**: Migrate to **LiteDB**. It fits the "Project file" paradigm perfectly while offering database reliability and partial loading capabilities.

### Example: Implementing Atomic Save (JSON)
```csharp
public void SaveProject(string path, Project project)
{
    string tempPath = path + ".tmp";
    string backupPath = path + ".bak";
    
    // 1. Write to Temp
    var json = JsonSerializer.Serialize(project);
    File.WriteAllText(tempPath, json);
    
    // 2. Create Backup
    if (File.Exists(path))
        File.Copy(path, backupPath, true);
        
    // 3. Move Temp to Final
    File.Move(tempPath, path, true);
}
```
