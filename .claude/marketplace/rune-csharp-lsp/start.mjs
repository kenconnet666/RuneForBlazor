// 启动仓库固定版本的 csharp-ls（dotnet-tools.json），经标准输入输出说 LSP。dotnet 由 tools/dotnet.mjs 找（要装着 global.json 所要的 SDK）。
// 生成器与代码修复先拷一份到 bin/ls/ 让语言服务器加载（Directory.Build.props 按 RuneLanguageServer 指过去）：
// 它加载了就锁住文件，锁住副本，正式构建往 bin/ 写生成器时才不会失败。
import { cpSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { root, runDotnet } from '../../../tools/dotnet.mjs';

for (const project of ['Rune.Generators', 'Rune.CodeFixes']) {
  const built = join(root, 'src', project, 'bin', 'Debug');
  try {
    if (existsSync(built)) cpSync(built, join(root, 'src', project, 'bin', 'ls', 'Debug'), { recursive: true, force: true });
  } catch {
    // 副本还被上一个语言服务器锁着：沿用已有的副本
  }
}

try {
  runDotnet(['tool', 'run', 'csharp-ls', '--', '--solution', 'RuneForBlazor.slnx', '--loglevel', 'warning'], { RuneLanguageServer: 'true' });
} catch (error) {
  process.stderr.write(`csharp-ls：${error.message}\n`);
  process.exit(1);
}
