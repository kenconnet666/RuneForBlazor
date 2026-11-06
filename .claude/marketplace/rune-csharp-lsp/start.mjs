// 启动仓库固定版本的 csharp-ls（dotnet-tools.json），经标准输入输出说 LSP。dotnet 由 tools/dotnet.mjs 找（要装着 global.json 所要的 SDK）。
// 生成器与代码修复先拷一份到 bin/ls/ 让语言服务器加载（Directory.Build.props 按 RuneLanguageServer 指过去）：
// 它加载了就锁住文件，锁住副本，正式构建往 bin/ 写生成器时才不会失败。
//
// 这里还是一层很薄的代理：csharp-ls 只监视 .cs / .cshtml / .csproj / .sln，新建、修改 .razor 它都不知道，组件生成的类型要等重启才认得。
// 代理自己监视仓库里的 .razor，变了（防抖）就在客户端发来的消息之间插一条“所属 .csproj 变了”的 workspace/didChangeWatchedFiles，
// csharp-ls 随之重新加载解决方案（本仓库约半分钟），不用重启。服务端到客户端的消息原样转发。
import { spawn } from 'node:child_process';
import { cpSync, existsSync, readdirSync, watch } from 'node:fs';
import { dirname, join, relative, sep } from 'node:path';
import { pathToFileURL } from 'node:url';
import { findDotnet, root } from '../../../tools/dotnet.mjs';

for (const project of ['Rune.Generators', 'Rune.CodeFixes']) {
  const built = join(root, 'src', project, 'bin', 'Debug');
  try {
    if (existsSync(built)) cpSync(built, join(root, 'src', project, 'bin', 'ls', 'Debug'), { recursive: true, force: true });
  } catch {
    // 副本还被上一个语言服务器锁着：沿用已有的副本
  }
}

let server;
try {
  const { dotnet, env } = findDotnet();
  server = spawn(dotnet, ['tool', 'run', 'csharp-ls', '--', '--solution', 'RuneForBlazor.slnx', '--loglevel', 'warning'], {
    cwd: root,
    env: { ...env, RuneLanguageServer: 'true' },
    stdio: ['pipe', 'inherit', 'inherit'],
  });
} catch (error) {
  process.stderr.write(`csharp-ls：${error.message}\n`);
  process.exit(1);
}
server.on('exit', (code, signal) => process.exit(code ?? (signal ? 1 : 0)));
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => server.kill(signal));

// 客户端 → 服务端：按 Content-Length 切成完整的消息再转发，插入的通知只会落在两条消息之间
let pending = Buffer.alloc(0);
process.stdin.on('data', (chunk) => {
  pending = Buffer.concat([pending, chunk]);
  for (;;) {
    const headerEnd = pending.indexOf('\r\n\r\n');
    if (headerEnd < 0) return;
    const length = Number(/Content-Length:\s*(\d+)/i.exec(pending.subarray(0, headerEnd).toString('ascii'))?.[1]);
    const end = headerEnd + 4 + length;
    if (Number.isNaN(length) || pending.length < end) return;
    server.stdin.write(pending.subarray(0, end));
    pending = pending.subarray(end);
  }
});
process.stdin.on('end', () => server.stdin.end());

function notify(method, params) {
  const body = Buffer.from(JSON.stringify({ jsonrpc: '2.0', method, params }));
  server.stdin.write(`Content-Length: ${body.length}\r\n\r\n`);
  server.stdin.write(body);
}

// .razor 所属的项目：从它所在的目录往上找第一个 .csproj
function projectOf(file) {
  for (let dir = dirname(file); dir.startsWith(root) && dir !== dirname(root); dir = dirname(dir)) {
    const csproj = readdirSync(dir).find((name) => name.endsWith('.csproj'));
    if (csproj) return join(dir, csproj);
  }
  return null;
}

const changedProjects = new Set();
let timer;
function onRazorChanged(file) {
  const project = projectOf(file);
  if (!project) return;
  changedProjects.add(project);
  clearTimeout(timer);
  // 防抖：一次生成、改名常常连着几个文件
  timer = setTimeout(() => {
    const changes = [...changedProjects].map((path) => ({ uri: pathToFileURL(path).href, type: 2 }));
    changedProjects.clear();
    notify('workspace/didChangeWatchedFiles', { changes });
  }, 1500);
}

for (const folder of ['src', 'apps', 'tests']) {
  const base = join(root, folder);
  if (!existsSync(base)) continue;
  try {
    watch(base, { recursive: true }, (_, name) => {
      if (!name?.endsWith('.razor')) return;
      const parts = name.split(sep);
      if (parts.includes('bin') || parts.includes('obj')) return;
      const file = join(base, name);
      if (relative(root, file).startsWith('..')) return;
      onRazorChanged(file);
    });
  } catch (error) {
    process.stderr.write(`rune-csharp-lsp：监视 ${folder} 失败，新建的 .razor 要重启语言服务器才认得（${error.message}）\n`);
  }
}
