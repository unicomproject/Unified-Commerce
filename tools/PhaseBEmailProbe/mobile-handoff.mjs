// Development-only bridge for the existing localhost invitation. Never logs URLs/tokens.
import http from 'node:http';
import { createReadStream, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const apk = fileURLToPath(new URL('../../../Tenantadmin/Nytroz-POS-App/build/app/outputs/flutter-apk/app-debug.apk', import.meta.url));
const page = `<!doctype html><html lang="en"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>ONEVERZ account setup</title>
<style>body{font:18px system-ui;background:#fafafa;color:#222;max-width:520px;margin:60px auto;padding:24px}a{display:block;padding:16px;margin:20px 0;background:#ed6100;color:white;text-decoration:none;border-radius:8px}p{line-height:1.6}[hidden]{display:none!important}</style>
<h1>Set up your Tenant Admin account</h1><p id="message">Open the ONEVERZ app to verify your invitation and set your password.</p>
<a id="open" hidden>Open app</a><a href="/download/app-debug.apk">Download Android test app</a>
<p>Install the app first, then reopen this invitation and tap Open app. This test requires the phone to stay connected to the development computer by USB.</p>
<script>
const parts=location.pathname.split('/');
let token='';try{if(parts.length===4&&parts[1]==='tenant-admin'&&parts[2]==='setup')token=decodeURIComponent(parts[3]);}catch{}
history.replaceState(null,'','/invitation');
if(/^[A-Za-z0-9_-]{20,2048}$/.test(token)){
const link=document.getElementById('open');link.href='oneverz://tenant-admin/setup?token='+encodeURIComponent(token);link.hidden=false;
}else{document.getElementById('message').textContent='Open the original invitation link from your email to continue.';}
</script></html>`;
const server=http.createServer((req,res)=>{
  res.setHeader('Cache-Control','no-store');
  res.setHeader('Referrer-Policy','no-referrer');
  res.setHeader('X-Content-Type-Options','nosniff');
  res.setHeader('Content-Security-Policy',"default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; base-uri 'none'; frame-ancestors 'none'");
  if(req.method!=='GET'){res.writeHead(405);res.end();return;}
  let url;try{url=new URL(req.url,'http://localhost:4200');}catch{res.writeHead(400);res.end();return;}
  if(url.pathname==='/download/app-debug.apk'){
    if(!existsSync(apk)){res.writeHead(404);res.end('Test app build not available.');return;}
    res.setHeader('Content-Type','application/vnd.android.package-archive');
    res.setHeader('Content-Disposition','attachment; filename="oneverz-development.apk"');
    createReadStream(apk).on('error',()=>res.destroy()).pipe(res);return;
  }
  if(url.pathname.startsWith('/tenant-admin/setup/')||url.pathname==='/invitation'){
    res.setHeader('Content-Type','text/html; charset=utf-8');res.end(page);return;
  }
  res.writeHead(404);res.end('Open your invitation email link.');
});
server.on('error',()=>{console.error('Handoff server could not start; check whether local port 4200 is already in use.');process.exitCode=1;});
server.listen(4200,'127.0.0.1',()=>console.log('Development handoff ready on localhost:4200. Request logging disabled.'));
