(() => {
  const canvas = document.getElementById('view');
  const gl = canvas.getContext('webgl', { antialias: false });
  if (!gl) throw new Error('WebGL is required for ProjectTwelve WebGL Viz');
  let payload = JSON.parse(document.getElementById('initial-payload').textContent);
  let scale = 24, offsetX = 24, offsetY = 24, dragging = false, last = null, selected = null;
  const toggles = {
    chunks: document.getElementById('chunkToggle'),
    solid: document.getElementById('solidToggle'),
    light: document.getElementById('lightToggle'),
  };
  const vs = `attribute vec2 p;attribute vec3 c;uniform vec2 r;varying vec3 v;void main(){vec2 z=p/r*2.0-1.0;gl_Position=vec4(z.x,-z.y,0,1);v=c;}`;
  const fs = `precision mediump float;varying vec3 v;void main(){gl_FragColor=vec4(v,1);}`;
  function shader(type, source){ const s=gl.createShader(type); gl.shaderSource(s, source); gl.compileShader(s); if(!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s)); return s; }
  const program = gl.createProgram(); gl.attachShader(program, shader(gl.VERTEX_SHADER, vs)); gl.attachShader(program, shader(gl.FRAGMENT_SHADER, fs)); gl.linkProgram(program); gl.useProgram(program);
  const pLoc = gl.getAttribLocation(program, 'p'), cLoc = gl.getAttribLocation(program, 'c'), rLoc = gl.getUniformLocation(program, 'r');
  const buffer = gl.createBuffer();
  function color(tile){ let [r,g,b]=tile.color.map(v=>v/255); if(toggles.light.checked){ r=g=b=Math.max(0.12, tile.light/15); } if(toggles.solid.checked && tile.solid){ r=Math.min(1,r+.18); g*=.65; b*=.65; } return [r,g,b]; }
  function pushRect(out,x,y,w,h,cc){ [[x,y],[x+w,y],[x,y+h],[x+w,y],[x+w,y+h],[x,y+h]].forEach(([px,py])=>out.push(px,py,...cc)); }
  function vertices(){ const out=[]; for(const t of payload.tiles){ const x=offsetX+(t.x-payload.bounds.xMin)*scale, y=offsetY+(payload.bounds.yMax-t.y)*scale; pushRect(out,x,y,scale,scale,color(t)); } if(toggles.chunks.checked){ const cc=[0.05,0.09,0.16]; const line=Math.max(1,scale*.08); for(let x=Math.floor(payload.bounds.xMin/32)*32;x<=payload.bounds.xMax+32;x+=32){ const sx=offsetX+(x-payload.bounds.xMin)*scale-line/2; pushRect(out,sx,offsetY,line,payload.height*scale,cc); } for(let y=Math.floor(payload.bounds.yMin/32)*32;y<=payload.bounds.yMax+32;y+=32){ const sy=offsetY+(payload.bounds.yMax-y)*scale-line/2; pushRect(out,offsetX,sy,payload.width*scale,line,cc); } } return new Float32Array(out); }
  function resize(){ const dpr=window.devicePixelRatio||1; const w=Math.floor(canvas.clientWidth*dpr), h=Math.floor(canvas.clientHeight*dpr); if(canvas.width!==w||canvas.height!==h){ canvas.width=w; canvas.height=h; } gl.viewport(0,0,canvas.width,canvas.height); }
  function render(){ resize(); gl.clearColor(.53,.81,.92,1); gl.clear(gl.COLOR_BUFFER_BIT); const data=vertices(); gl.bindBuffer(gl.ARRAY_BUFFER, buffer); gl.bufferData(gl.ARRAY_BUFFER, data, gl.DYNAMIC_DRAW); gl.enableVertexAttribArray(pLoc); gl.vertexAttribPointer(pLoc,2,gl.FLOAT,false,20,0); gl.enableVertexAttribArray(cLoc); gl.vertexAttribPointer(cLoc,3,gl.FLOAT,false,20,8); gl.uniform2f(rLoc,canvas.width,canvas.height); gl.drawArrays(gl.TRIANGLES,0,data.length/5); updateChrome(); }
  function tileAt(clientX, clientY){ const rect=canvas.getBoundingClientRect(); const x=Math.floor((clientX-rect.left-offsetX)/scale)+payload.bounds.xMin; const y=payload.bounds.yMax-Math.floor((clientY-rect.top-offsetY)/scale); return payload.tiles.find(t=>t.x===x&&t.y===y) || { x, y, id:0, name:'Air', light:15, fluid:0, solid:false }; }
  function inspect(t){ selected=t; document.getElementById('inspector').textContent=JSON.stringify(t,null,2); }
  function updateChrome(){ document.getElementById('title').textContent=payload.name; document.getElementById('meta').textContent=`${payload.kind} ${payload.width}×${payload.height} · ${payload.source || 'dropped JSON'} · zoom ${scale.toFixed(1)}px/tile`; const ids=[...new Map(payload.tiles.map(t=>[t.id,t])).values()].sort((a,b)=>a.id-b.id); document.getElementById('legend').innerHTML=ids.map(t=>`<div><div class="swatch" style="background:rgb(${t.color.join(',')})"></div><span class="muted">${t.id} ${t.name}</span></div>`).join(''); }
  canvas.addEventListener('mousedown', e=>{dragging=true; last=[e.clientX,e.clientY];}); window.addEventListener('mouseup',()=>dragging=false); window.addEventListener('mousemove',e=>{ if(!dragging) return; offsetX+=e.clientX-last[0]; offsetY+=e.clientY-last[1]; last=[e.clientX,e.clientY]; render(); }); canvas.addEventListener('wheel',e=>{ e.preventDefault(); const before=tileAt(e.clientX,e.clientY); scale=Math.max(4,Math.min(96,scale*(e.deltaY<0?1.12:.88))); const rect=canvas.getBoundingClientRect(); offsetX=e.clientX-rect.left-(before.x-payload.bounds.xMin)*scale; offsetY=e.clientY-rect.top-(payload.bounds.yMax-before.y)*scale; render(); }); canvas.addEventListener('click',e=>inspect(tileAt(e.clientX,e.clientY))); Object.values(toggles).forEach(el=>el.addEventListener('change',render));
  document.getElementById('fileInput').addEventListener('change', async e=>{ const file=e.target.files[0]; if(!file) return; payload=JSON.parse(await file.text()); payload.source=file.name; offsetX=24; offsetY=24; render(); });
  document.getElementById('exportBtn').addEventListener('click',()=>{ const report={ tool:'project-twelve/webgl-viz', payload:payload.name, source:payload.source, bounds:payload.bounds, viewport:{ scale, offsetX, offsetY }, overlays:{ chunks:toggles.chunks.checked, solid:toggles.solid.checked, light:toggles.light.checked }, selected }; const a=document.createElement('a'); a.href=URL.createObjectURL(new Blob([JSON.stringify(report,null,2)],{type:'application/json'})); a.download=`${payload.name || 'webgl-viz'}-evidence.json`; a.click(); });
  window.addEventListener('resize',render); render();
})();
