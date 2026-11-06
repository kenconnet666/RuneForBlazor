// 用装着 global.json 所要 SDK 的 dotnet 执行命令：node tools/dotnet.mjs run --project apps/Rune.Docs …
// PATH 上的 dotnet 可能只有别的版本（SDK 装在用户目录 ~/.dotnet 时常见），按 DOTNET_ROOT → ~/.dotnet → PATH 的顺序找第一个有对应主版本 SDK 的；
// 子进程的 DOTNET_ROOT 与 PATH 也指向它（csharp-ls 的 MSBuildLocator、dotnet run 起的应用按它找运行时）。
// Claude Code 的 launch.json 与 C# LSP 插件都经这里启动。
import { spawn, spawnSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { delimiter, dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

/** 仓库根目录 */
export const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');

/** 找到的 dotnet 与给子进程用的环境变量；找不到时抛错 */
export function findDotnet() {
  const wanted = JSON.parse(readFileSync(join(root, 'global.json'), 'utf8')).sdk.version.split('.')[0];
  const exe = process.platform === 'win32' ? 'dotnet.exe' : 'dotnet';
  const candidates = [process.env.DOTNET_ROOT && join(process.env.DOTNET_ROOT, exe), join(homedir(), '.dotnet', exe)]
    .filter((path) => path && existsSync(path))
    .concat('dotnet');
  const hasSdk = (dotnet) => {
    const result = spawnSync(dotnet, ['--list-sdks'], { encoding: 'utf8' });
    return result.status === 0 && result.stdout.split('\n').some((line) => line.startsWith(`${wanted}.`));
  };
  const dotnet = candidates.find(hasSdk);
  if (!dotnet) throw new Error(`找不到装着 .NET ${wanted} SDK 的 dotnet（试过 ${candidates.join('、')}）`);
  const env = { ...process.env };
  if (dotnet !== 'dotnet') {
    env.DOTNET_ROOT = dirname(dotnet);
    env.PATH = `${dirname(dotnet)}${delimiter}${env.PATH ?? ''}`;
  }
  return { dotnet, env };
}

/** 在仓库根目录用找到的 dotnet 执行命令，标准输入输出直通，退出码跟着子进程 */
export function runDotnet(args) {
  const { dotnet, env } = findDotnet();
  const child = spawn(dotnet, args, { cwd: root, env, stdio: 'inherit' });
  child.on('exit', (code, signal) => process.exit(code ?? (signal ? 1 : 0)));
  for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => child.kill(signal));
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    runDotnet(process.argv.slice(2));
  } catch (error) {
    process.stderr.write(`${error.message}\n`);
    process.exit(1);
  }
}
