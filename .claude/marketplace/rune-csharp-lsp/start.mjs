// 启动仓库固定版本的 csharp-ls（dotnet-tools.json），经标准输入输出说 LSP。
// 要找一个装着 global.json 所要 SDK 的 dotnet：PATH 上的那个可能只有别的版本（SDK 装在用户目录 ~/.dotnet 时常见），
// 按 DOTNET_ROOT → ~/.dotnet → PATH 的顺序找第一个有对应主版本 SDK 的。csharp-ls 用 MSBuildLocator 加载同一个 dotnet 下的 SDK，
// 所以子进程的 DOTNET_ROOT 与 PATH 也指向它。
import { spawn, spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const wanted = JSON.parse(readFileSync(join(root, 'global.json'), 'utf8')).sdk.version.split('.')[0];
const exe = process.platform === 'win32' ? 'dotnet.exe' : 'dotnet';

function hasSdk(dotnet) {
  const result = spawnSync(dotnet, ['--list-sdks'], { encoding: 'utf8' });
  return result.status === 0 && result.stdout.split('\n').some((line) => line.startsWith(`${wanted}.`));
}

const candidates = [process.env.DOTNET_ROOT && join(process.env.DOTNET_ROOT, exe), join(homedir(), '.dotnet', exe)]
  .filter((path) => path && existsSync(path))
  .concat('dotnet');
const dotnet = candidates.find(hasSdk);
if (!dotnet) {
  process.stderr.write(`csharp-ls：找不到装着 .NET ${wanted} SDK 的 dotnet（试过 ${candidates.join('、')}）\n`);
  process.exit(1);
}

const env = { ...process.env };
if (dotnet !== 'dotnet') {
  env.DOTNET_ROOT = dirname(dotnet);
  env.PATH = `${dirname(dotnet)}${process.platform === 'win32' ? ';' : ':'}${env.PATH ?? ''}`;
}
const child = spawn(dotnet, ['tool', 'run', 'csharp-ls', '--', '--solution', 'RuneForBlazor.slnx', '--loglevel', 'warning'], {
  cwd: root,
  env,
  stdio: 'inherit',
});
child.on('exit', (code, signal) => process.exit(code ?? (signal ? 1 : 0)));
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => child.kill(signal));
