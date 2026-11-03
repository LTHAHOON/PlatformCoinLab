# AgentVFX

Editor-only JSON to VFX prefab pipeline. The flow is:

1. Validate JSON and compute a stable hash.
2. Generate PNG texture, mesh asset, URP material, Particle System, and prefab.
3. Validate references, required components, and shader support.

Generated files are placed under Assets/AgentVFX/Generated/<effect-id>-<spec-hash>/. Re-running the same specification reuses the hashed assets; changing the specification creates a new directory.
