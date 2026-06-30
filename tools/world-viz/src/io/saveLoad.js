// Parse a SandboxSaveData JSON save game (written by Unity JsonUtility).
//
// Schema (Assets/Scripts/Sandbox/SandboxSaveData.cs):
//   { "version": 1, "seed": <int>,
//     "chunks": [ { "x": <int>, "y": <int>,
//                   "edits": [ { "localX": <int>, "localY": <int>,
//                                "tile": { "id", "light", "fluid", "metadata" } } ] } ] }
// Only edited chunks are stored; everything else is regenerated from `seed`.

/**
 * Normalise raw parsed JSON into { version, seed, chunks } with well-typed
 * fields. Tolerant of missing optional fields but strict about structure.
 */
export function parseSave(json) {
  if (json == null || typeof json !== 'object') {
    throw new Error('Save file is not a JSON object.');
  }
  if (typeof json.seed !== 'number') {
    throw new Error('Save file is missing a numeric "seed".');
  }
  const chunksRaw = Array.isArray(json.chunks) ? json.chunks : [];
  const chunks = chunksRaw.map((c, i) => {
    if (typeof c.x !== 'number' || typeof c.y !== 'number') {
      throw new Error(`chunks[${i}] is missing numeric x/y.`);
    }
    const editsRaw = Array.isArray(c.edits) ? c.edits : [];
    const edits = editsRaw.map((e, j) => {
      const t = e.tile || {};
      if (typeof e.localX !== 'number' || typeof e.localY !== 'number') {
        throw new Error(`chunks[${i}].edits[${j}] is missing numeric localX/localY.`);
      }
      return {
        localX: e.localX | 0,
        localY: e.localY | 0,
        tile: {
          id: (t.id ?? 0) | 0,
          light: (t.light ?? 0) | 0,
          fluid: typeof t.fluid === 'number' ? t.fluid : 0,
          metadata: (t.metadata ?? 0) | 0,
        },
      };
    });
    return { x: c.x | 0, y: c.y | 0, edits };
  });
  return {
    version: typeof json.version === 'number' ? json.version : 1,
    seed: json.seed | 0,
    chunks,
  };
}

/** Parse a save from a JSON string. */
export function parseSaveText(text) {
  let json;
  try {
    json = JSON.parse(text);
  } catch (err) {
    throw new Error(`Save file is not valid JSON: ${err.message}`);
  }
  return parseSave(json);
}
