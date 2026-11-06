// 启动仓库固定版本的 csharp-ls（dotnet-tools.json），经标准输入输出说 LSP。dotnet 由 tools/dotnet.mjs 找（要装着 global.json 所要的 SDK）。
import { runDotnet } from '../../../tools/dotnet.mjs';

try {
  runDotnet(['tool', 'run', 'csharp-ls', '--', '--solution', 'RuneForBlazor.slnx', '--loglevel', 'warning']);
} catch (error) {
  process.stderr.write(`csharp-ls：${error.message}\n`);
  process.exit(1);
}
