; 尚未发布的诊断

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RUNE001 | Rune | Error | [State] 不能是静态的
RUNE002 | Rune | Error | [State] 需要 get 与 set
RUNE003 | Rune | Error | [State] 不能写在 .razor 的 @code 里
RUNE004 | Rune | Warning | 静态 Style 成员应该用 Css.Define 定义
RUNE005 | Rune | Warning | 不要在渲染路径上新建 Style
RUNE006 | Rune | Info | 样式值有现成的常量或类型化写法
RUNE007 | Rune | Disabled | 组件内部的长度不要用 px
RUNE008 | Rune | Warning | for 循环变量被延后执行的内容捕获
RUNE009 | Rune | Error | 不要在静态初始化里调用 Attrs.Merge
