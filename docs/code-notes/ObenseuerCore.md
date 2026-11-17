# ObenseuerCore code notes

- `src/ComputerApps.cs:ClosePinned` matches both app ID and pinned target before forced overlay cleanup, avoiding `ComputerAppsDesk.Hide` for unrelated desk hosts. `checks/ComputerApps/Program.cs:Main` covers mismatched IDs and targets, guard bypass, event cleanup and desk-hide isolation.
