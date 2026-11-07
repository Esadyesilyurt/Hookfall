// Hookfall - read/write helpers for Shader Graph multi-JSON files. AI disclosure: written with Claude (Anthropic) on 2026-11-07.
const fs = require('fs');
const BS = String.fromCharCode(92);

function parse(file) {
  const text = fs.readFileSync(file, 'utf8');
  const objs = []; let depth = 0, start = -1, inStr = false, esc = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inStr) { if (esc) esc = false; else if (c === BS) esc = true; else if (c === '"') inStr = false; continue; }
    if (c === '"') { inStr = true; continue; }
    if (c === '{') { if (depth === 0) start = i; depth++; }
    else if (c === '}') { depth--; if (depth === 0) objs.push(JSON.parse(text.slice(start, i + 1))); }
  }
  return objs;
}

function write(file, objs) {
  fs.writeFileSync(file, objs.map(o => JSON.stringify(o, null, 4)).join('\n\n') + '\n');
}

function dump(objs) {
  const byId = Object.fromEntries(objs.map(o => [o.m_ObjectId, o]));
  const g = objs[0];
  for (const o of objs) {
    const t = o.m_Type.split('.').pop();
    if (/Node$/.test(t)) {
      const slots = (o.m_Slots || []).map(s => { const so = byId[s.m_Id]; return `${so.m_Id}:${so.m_DisplayName}(${so.m_SlotType === 1 ? 'out' : 'in'})`; });
      console.log('NODE', o.m_ObjectId, t, o.m_Name, JSON.stringify(o.m_Property || o.m_SerializedDescriptor || o.m_SubGraph || ''), slots.join(' | '));
    } else if (/ShaderProperty$/.test(t)) console.log('PROP', o.m_ObjectId, t, o.m_Name, o.m_DefaultReferenceName, JSON.stringify(o.m_Value));
    else if (!/Slot$/.test(t)) console.log('OBJ', o.m_ObjectId, t);
  }
  for (const e of g.m_Edges) {
    const os = byId[e.m_OutputSlot.m_Node.m_Id], is = byId[e.m_InputSlot.m_Node.m_Id];
    console.log('EDGE', os.m_Name, e.m_OutputSlot.m_SlotId, '->', is.m_Name, e.m_InputSlot.m_SlotId);
  }
}

module.exports = { parse, write, dump };
if (require.main === module) dump(parse(process.argv[2]));
