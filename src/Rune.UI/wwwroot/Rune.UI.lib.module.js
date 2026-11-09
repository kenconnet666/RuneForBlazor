// Rune.UI 的 JS 初始化器：Blazor 启动时按文件名约定（{程序集名}.lib.module.js）自动加载，静态 SSR、Server、WebAssembly 都会运行。
//
// 浮层尽量交给浏览器原生：popover 属性（顶层、点外面与 Esc 关闭、popovertarget 打开）、<dialog>（模态、焦点圈在里面、关闭后焦点还给触发元素）、
// @starting-style 进出场。组件不直接调用 JS，只在元素上写属性，这里用全局的事件委托补原生没有的几样：
//
//   data-rune-overlay        浮层（popover 元素或 dialog）：开关时发出冒泡的 rune-toggle（Blazor 里是 @onrunetoggle）
//   data-open="true|false"   受控的浮层：属性变了就打开或关闭（组件的 Open 参数）
//   data-rune-open="id"      点击时打开这个 id 的浮层（对话框的触发按钮；popover 用原生的 popovertarget）
//   data-rune-close          点击时关闭所在的浮层（对话框的取消按钮、关闭按钮）
//   data-rune-floating="bottom-start"
//                            锚定浮层（popover 元素）：打开时用 Floating UI 贴着触发元素（popovertarget、data-rune-tooltip 或 data-rune-hovercard 指向它的元素）
//                            定位并持续跟随，空间不够时翻转、贴着视口平移；data-offset 是距离，data-fit-height 时最大高度限制在视口剩下的空间里；
//                            最终方向写到 data-side / data-align；里面的 data-rune-arrow 是箭头。定位好之后才可见，菜单聚焦第一项，
//                            对话框类的浮层聚焦 autofocus 或第一个可聚焦元素
//   data-rune-close-on-link  对话框：点了里面的链接（通常是换页）就关上（AppShell 窄屏时的导航抽屉）
//   data-rune-close-when="(min-width: 64rem)"
//                            对话框：媒体查询成立时关上（窗口变宽、侧栏常驻后导航抽屉没必要开着）
//   data-rune-fullscreen="true|false"
//                            应用外壳（AppShell）的全屏：变成 true 时请求浏览器全屏，false 时退出；用户自己离开浏览器全屏、
//                            或没进浏览器全屏时按 Esc，发出冒泡的 rune-command（fullscreen-exit）
//   data-rune-theme-switcher 主题切换的菜单：里面 data-rune-theme-choice="名字" 的项点了就切换主题（记在 localStorage、写根元素的 data-theme，
//                            从触发按钮处圆形展开），选中项的 aria-checked 与触发按钮里 data-rune-theme-icon 的图标按当前选择同步
//                            （配置是 ThemeBootScript 输出的 script#rune-theme-config）
//   data-rune-token-value="--变量名"  显示主题 token 当前取值的元素（ColorSwatch）：插入时与换主题后读出来写进去
//   data-rune-light-dismiss  对话框：点遮罩关闭（原生的 closedby="any" 还没普及）
//   data-rune-guard-close    对话框：要关闭时（Esc、点遮罩、data-rune-close）不直接关，发出冒泡的 rune-close-request
//                            （Blazor 里是 @onrunecloserequest，带原因 escape / outside / close），由 C# 决定关不关（确认中、关闭前确认）
//   role="menu"              菜单：方向键、Home / End、类型搜索移动焦点；打开时聚焦第一项（触发按钮上按 ↑ 打开时聚焦最后一项，↓ 也能打开）；
//                            选了菜单项就关闭整串菜单（data-rune-keep-open 的不关）；data-rune-hotkey 的菜单项按下快捷键时点击它；
//                            子菜单项（aria-haspopup="menu"）点击、→ 打开并聚焦第一项，指针停留打开、斜着移向子菜单不误关（安全三角），子菜单里 ← 关闭
//   data-rune-cascader       级联选择的弹层：每一列是 data-rune-list 的列表；→ 进入下一级（发 rune-command enter，下一列渲染出来后聚焦）、
//                            ← 回到上一级，有下一级的项上 Enter 先点再进入，高亮移动后发 highlight（C# 换下一列）；触发按钮上 ↓ 打开
//   data-rune-mentions="@"  提及的文本框：输入 @ 弹出列表（贴着 @ 的位置），@ 后面的文字发 rune-command（mention-query）给 C# 过滤；
//                            ↑↓ 移动、Enter / Tab 选中、Esc 关闭；选中后把“@过滤词”换成“@名字 ”并发 change
//   data-rune-option-dblclick 列表：双击选项时发出 rune-command（move，带序号），穿梭框据此直接移过去
//   data-rune-context-menu   右键菜单的区域：右键、Shift+F10、菜单键时从指针位置（键盘时从焦点元素的左下角）弹出里面的菜单，关闭后焦点回到原处
//   data-rune-list="space typeahead"
//                            列表的键盘宿主：role="listbox" 本身，或 aria-controls 指向列表的触发按钮、输入框。焦点留在宿主上，
//                            ↑↓、Home / End、Page Up / Down 移动高亮（宿主的 data-rune-active 与 aria-activedescendant、选项的 data-highlighted），
//                            Enter（space 时还有空格）点击高亮项（C# 处理选中），typeahead 时按字母跳到以它开头的项；
//                            列表自己获得焦点时高亮第一个已选项（没有就第一个可用项）；指针移过选项时高亮它。
//                            虚拟滚动的列表带 data-rune-options（全部选项的文字与不可用）与 data-rune-selected（第一个已选项的序号）。
//                            text 时宿主是输入框（搜索框）：Home / End、空格、字母留给输入
//   选项弹出层（Select）     触发按钮是带 popovertarget 的列表宿主：关闭时 ↓ / Enter / 空格打开（↑ 打开并高亮最后一项），单选时按字母直接选中；
//                            打开后高亮默认项，Tab 关闭；单选列表选中一项后关闭；关闭时焦点在弹层里就还给触发按钮。
//                            触发按钮的 aria-expanded 与外壳（data-rune-control）的 data-state 在开关时同步；点外壳的空白处与点触发按钮一样
//   data-anchor="id"         锚定浮层贴着这个 id 的元素定位（Select 的弹层贴着输入框外壳），而不是触发元素
//   data-rune-query="词"      弹层里的列表：搜索词变了时高亮第一个匹配项，停顿后播报 data-rune-results（“12 个选项”）
//   data-rune-backspace="命令" 按 Backspace（输入框里是空的时候）发出冒泡的 rune-command（Blazor 里是 @onrunecommand，带命令名）：
//                            多选下拉移除最后一个已选项
//   data-rune-escape="命令"   弹层关着时在有字的输入框里按 Esc：发出 rune-command（Combobox 清空）
//   data-rune-enter="命令"    在有字的输入框里按 Enter、列表没有用掉它时：发出 rune-command（TagsInput 加成标签）
//   data-rune-paste="命令"    粘贴的内容里有 data-rune-delimiters 里的分隔符或换行时拦下，整段随 rune-command 交给 C#（带 text）
//   data-rune-popover="id"   输入框做列表宿主时它的弹层（输入框不能做 popovertarget）：输入时打开，↑↓ 打开并发 show-all 命令
//   data-rune-hide-empty     这样的弹层只在输入过、且有建议（或正在加载，aria-busy）时显示（Autocomplete）
//   输入法组合期间           列表宿主输入框（data-rune-list 含 text）的 input 事件拦下，组合结束补发一次：不拿拼音字母去过滤
//   data-rune-roving="horizontal|vertical|both"
//                            漫游焦点：整组一个 Tab 位置，方向键、Home / End 在组内移动（项是 data-rune-roving-items，默认 button），
//                            tabindex 由 JS 写；可加 noloop（到两端不循环）
//   data-rune-indicator="选择器" 跟着当前项（父元素里匹配选择器的项）走的指示条：选中变化时滑过去，尺寸变化时直接对齐；data-axis 是方向
//   data-rune-slider         滑块：按下（轨道、滑块）、拖动、方向键 / Page Up / Down / Home / End 在这里算值并当场画出来，
//                            发出冒泡的 rune-slide（Blazor 里是 @onruneslide，带 index、value、commit、seq）；data-min / data-max / data-step /
//                            data-big-step / data-orientation 是取值规则，data-values（C# 认定的值）与 data-ack（C# 处理到的序号）由 C# 写
//   data-rune-rating         评分：指针移到某个分数（label[data-score]）上时给各颗星写行内的 --rune-rating-fill 预览，移出或点了之后去掉
//   data-rune-otp            验证码输入框：输入时先过滤（data-charset、data-length），再按输入框的值画各格的 data-char 与当前格的 data-active
//   data-rune-scroll-area    滚动区域：浮层滚动条的大小与位置、显示与淡出、拖动、点轨道翻页、滚轮转交；内容超出时视口可聚焦（data-focusable）
//   data-rune-page-scrollbar 整页的浮层滚动条（PageScrollbar）：同上，视口是整个窗口
//   data-rune-collapse       折叠内容区：收起的过渡结束后加 hidden="until-found"，页内查找命中时发 rune-toggle（open: true）让 C# 展开
//   data-rune-image          图片（Avatar、Image）：加载完写 data-state="loaded"（显示出来），失败写 "error"（藏起来，露出后备内容）；插入时已经加载完的照样标
//   data-rune-lightbox-viewport 图片预览（Lightbox）的图片区：拖动平移，滚轮、捏合缩放，双击放大 / 放回，+ / − / 0 / ↑↓ 键；
//                            同一个 dialog 里 data-rune-lightbox-action 的按钮缩放、旋转、还原，← / → 点 data-rune-lightbox-step 的按钮（C# 换图）；
//                            图片（data-rune-lightbox-image）加载好、打开、旋转后整张放进视口，比例写进 data-rune-lightbox-zoom
//   data-rune-resize         宽度调整手柄（role="separator"）：拖动、方向键、双击回到 data-default，当场改父元素的宽（横向手柄是高），
//                            结束时发出冒泡的 rune-resize（size）；没给宽度时按父元素量出 aria-valuenow
//   data-rune-sortable="vertical|horizontal|grid" 可拖动排序的列表（Sortable；grid 是 Upload 的图片墙）：项（data-rune-sortable-item="key"）的把手（data-rune-sortable-handle）上拖动，
//                            其余项用 transform 让位，松开发 rune-reorder（新的顺序），顺序跟上后去掉位移；空格拿起、方向键挪、Esc 取消，每一步发 rune-reorder；
//                            data-texts 是读屏念的句子
//   data-rune-splitter-handle 分栏（Splitter）的分隔条：拖动、方向键（5%）、Home / End、双击回到 data-default，当场改外层（data-rune-splitter）的
//                            --rune-split-start / -end，结束时发 rune-resize（起始一栏的百分比）；外层有 data-collapsible 时 Enter 发 rune-command（toggle）
//   data-rune-overflow       一行标签（Select 的 MaxTagCount 按宽度）：量出放得下几个 data-rune-overflow-item，其余写 data-overflow-hidden
//                            （CSS 藏起来但仍可测量），data-rune-overflow-rest（“+N”）的文字取自 data-rune-more 里的第 N 项
//   data-rune-tabs           横排的标签栏：放不下时写 data-overflowing、启用滚动按钮，当前标签滚进来；可关闭的标签上 Delete、中键关闭；
//                            列表带 data-reorderable 时拖动标签（或 Alt + ←→）改顺序，发出冒泡的 rune-reorder（values）
//   data-rune-toc            本页目录：滚动时给当前节的链接写 aria-current="location"（data-scroll-root 是滚动容器的 id）
//   data-rune-loading-bar    顶部加载条：按 data-status / data-run 开始、完成、出错（延迟显示、先快后慢地涨）；带 data-navigation 时跟着增强导航走
//   data-rune-hotkey="mod+k" 对话框（命令面板）：按下快捷键时打开或关上；显示快捷键的元素写 data-rune-hotkey-label（macOS 上换成 ⌘）
//   data-rune-highlight="语言" 代码块的 code 元素：插入后用 Shiki 上色（第一次用到时才加载 Shiki 与这种语言的语法）
//   data-rune-tooltip="id"   提示的触发元素：指针停留 data-delay 毫秒后显示这个 id 的提示（刚关掉一个时直接显示），离开 100ms 后隐藏
//                            （可以移到提示上），键盘聚焦时立即显示；失焦、按下、Esc 隐藏；同一时间只显示一个
//   data-rune-hovercard="id" 悬停卡片的触发元素：同提示（停留 data-delay、离开 data-close-delay），但焦点可以移进卡片，离开触发元素与卡片才隐藏
//   data-indeterminate       勾选框的半选：原生的 indeterminate 只能用 JS 设置，按这个属性同步
//   data-rune-focus="id"     点击时聚焦这个 id 的控件并滚到它（表单错误汇总里的链接）
//   data-rune-toaster        通知区域：F8 跳进来；通知（data-rune-toast）里按 Esc、横向滑走时发出 rune-command（dismiss），焦点交给相邻的通知；
//                            焦点进出区域时在区域上发出 rune-command（focus-enter / focus-leave，C# 暂停、继续计时）
//   data-rune-pointer="lg"   跟随指针的装饰（卡片的倾斜、聚光）：元素上写阻尼后的 --rune-pointer-x / -y / -strength（弹簧时长档位，data-rune-pointer-bounce 回弹）
//   data-rune-leave-guard    有未保存的改动（LeaveConfirmation）：点了去本站别的页面的链接先不走，发出 rune-command（leave，带地址）由 C# 确认
//   data-rune-invalid-submit 表单：提交没通过时（计数变了）焦点到错误汇总（data-rune-error-summary），没有汇总时到第一个出错的控件
//   data-rune-spin           数字输入框：↑↓ 加减步长、Page Up / Down 加减大步长、Home / End 到最小 / 最大，改完发 change（C# 再校正）
//   data-rune-ripple         按钮、菜单项：按下时从指针位置（键盘 Enter / 空格时从中心）扩散一圈波纹，松开后淡出
//   data-rune-inert          加载中、可聚焦的不可用按钮：拦下点击的默认动作（提交表单）
//   data-rune-copy="文字"     复制按钮：点击时（data-rune-copy-on-click）或 data-rune-copy-request 变了时把文字写入剪贴板，
//                            成功后按钮的 data-state 变成 copied（data-rune-copy-timeout 毫秒后复原）、播报 data-rune-copied-text，
//                            发出冒泡的 rune-copied（Blazor 里是 @onrunecopied）
//   data-rune-observe-size   宽度变了（取整后）发出冒泡的 rune-resize（Blazor 里是 @onruneresize，size 是宽度）：C# 按宽度排版（虚拟网格每行几个）
//   data-rune-affix="top|bottom" 固钉（Affix，CSS sticky）：前面（贴底时后面）零高度的哨兵（data-rune-affix-sentinel）滚出去就写 data-stuck；
//                            data-target 是 sticky 相对的滚动容器的 id；data-rune-affix-report 时贴住、离开发出 rune-command（stick / unstick）
//   data-rune-back-top="400" 回到顶部（BackTop）：滚过这么多 px 写 data-shown、去掉 aria-hidden；点了平滑滚回顶部、焦点回到开头（data-target 是滚动容器）
//   data-rune-ellipsis-text  截断的文字（Ellipsis）：内容超出了就在外层（data-rune-ellipsis）写 data-truncated；外层是 "tooltip" 时截断的文字可聚焦，
//                            它上面的提示（data-rune-tooltip）只在截断时弹出；展开着（data-expanded）时不量
//   data-rune-marquee="40"  跑马灯（Marquee）：一份内容（data-rune-marquee-group）的长度除以速度（px/s）写成 --rune-marquee-duration，内容变了重新量；
//                            减少动态效果时容器可以聚焦（不滚动，手动滚动查看）
//   data-rune-watermark='{…}' 水印（Watermark）的那一层：按选项（文字或图片、角度、间隔、字号、颜色）用 canvas 画一块平铺单元写成背景，选项变了重画
//   data-rune-carousel        轮播（Carousel）：箭头、页点滚到那一张，滚动停下时写回序号（rune-command carousel-index），自动播放与暂停
//   data-rune-signature       手写签名（SignaturePad）：canvas 上按速度粗细写，撤销、清空；每写完一笔把 PNG 写进隐藏输入框并发 rune-command signature
//   data-rune-viewport-fill  占满到窗口底边的区域（ViewportFill）：量出自己的顶边写成 --rune-fill-top，窗口与文档尺寸变化时重新量
//   data-rune-grid-span="n"  网格里跨 n 列的项：网格实际的列比 n 少时（按宽度自动换列的网格变窄）改成跨当前的列数
//   data-rune-control        输入框的外壳：点在空白处（内边距、图标、前后缀）时聚焦里面的输入元素；
//                            框里不在 Tab 顺序里的按钮（数字框的加减）按下时不抢走输入框的焦点
//   data-rune-repeat         按住时连续点击（数字框的加减）：停一会儿后每隔一小段时间再点一次，松开为止
//   data-rune-refocus="id"   点击后把焦点交给这个 id 的元素（清空按钮清空后回到输入框）
//   data-rune-reveal="id"    显示密码按钮：切换这个 id 的输入框的 type（password / text）与按钮的 aria-pressed
//   data-rune-count="id"     有字数计数的输入框：输入时更新这个 id 的计数（看得见的字数与超出上限的 data-over）
//   data-rune-format="3,4,4" 分组显示的输入框（TextField 的 Format）：输入时只留接受的字符（data-rune-format-accept）、截到最长（-max）、
//                            按组加分隔（-separator），光标留在刚输入的字符后面
//   data-rune-autosize       多行文本框：输入时、值变了（属性值随之变化）时按 scrollHeight 设高度（上下限由 CSS 的 min/max-height 夹住）
//   data-rune-check-draw     复选框：用户点击勾上时把后面的对勾一笔画出（初次渲染、外部改值不画）
//   data-rune-draw="true"    图标的描线动画：插入时为 true、或由 false 变为 true 时一笔画出（减少动态效果时直接画好）
//   data-rune-file-drop      选文件的区域（FileDrop）：拖着文件经过时写 data-dragging；文件由盖满区域的原生 file input 接住
//   data-rune-upload-previews="地址 …" Upload 的图片墙：选到的图片建本地地址（object URL），经 rune-command（file-previews）交给 C#；
//                            属性值是还在用的地址，不再用的、组件不在了的收回
//   data-rune-paste-files    粘贴文件（截图）：塞进里面 FileDrop 的 input 并发 change，同选文件
//   data-rune-tween="lg"     会滚动的数字（AnimatedNumber）：data-value 变了时从显示中的值滚到新值（第一次出现时从 data-from），逐帧改文字节点，
//                            停下时换回 C# 的终值；中间帧按 data-locale、data-format（Intl 的选项加 scale、suffix）格式化，时长取主题的这一档
//   data-rune-dismiss        可移除元素（data-rune-dismissible，如标签）里的移除按钮：元素先离场（data-dismissing），
//                            结束后发出冒泡的 rune-dismiss（Blazor 里是 @onrunedismiss）；被移除时焦点交给同组的下一个
//                            （没有就上一个）可聚焦元素，没被移除就重新显示
//   data-rune-ready          根元素：初始化器装好了（端到端测试等它）
//   data-rune-theme="名字"   RuneProvider 的包裹元素：所用主题的名字（只做标记，调试与测试看）
//
// 模块内部的结构标记（含义见各模块开头的注释）：scroll-area 的 data-rune-scroll-viewport / -scrollbar，tabs 的 data-rune-tab-scroll / -tab-close，
// cascader 的 data-rune-cascader-trigger / -search / -column，mentions 的 data-rune-mentions-panel，hover-layers 的 data-rune-hover-panel，
// floating、menus 的 data-rune-submenu，highlight 的 data-rune-code-text，toast 的 data-rune-toast-close，theme 的 data-rune-choice-icon，
// shared 的 data-rune-announcer（共用的播报区）；分组输入框另有 data-rune-format-upper（转大写）。
//
// 模态对话框打开时锁定页面滚动。
//
// 这个文件（入口）注册自定义事件，js/core/runtime.js 挂全局的事件委托与 DOM 变化监听，再分给 js/ 下各模块：
// overlays（浮层开关、对话框）、menus（菜单的键盘、子菜单、右键菜单）、floating（定位）、lists（列表与选项弹出层）、hover-layers（提示与悬停卡片）、overflow（标签收起）、
// roving（漫游焦点）、indicator（跟随当前项的指示条）、slider（滑块）与 move（拖动）、rating（评分的悬停预览）、otp（验证码的格子）、scroll-area（浮层滚动条）、collapse（折叠内容区）、resize（宽度调整手柄、分栏的分隔条）、highlight（代码高亮）、tabs（标签栏的溢出滚动与关闭）、toc（本页目录的滚动高亮）、loading-bar（顶部加载条）、hotkeys（打开浮层的快捷键）、fullscreen（应用外壳的全屏）、theme（主题切换）、
// leave（离开确认）、cascader（级联选择的列）、mentions（@ 提及）与 caret（光标的坐标）、toast（通知区域）、files（选文件区域的拖放高亮）、tween（会滚动的数字）、affix（固钉与回到顶部）、watermark（水印）、sortable（拖动排序）、lightbox（图片预览的平移缩放）、pointer（跟随指针的装饰）、controls（输入框一类）、effects（复制、波纹、描线、离场）、layout（网格、尺寸观察、占满到窗口底边、截断的文字、跑马灯的时长）、shared（共用的小工具）。
//
// 加载：Blazor 启动前要等初始化器（连同它静态导入的模块）加载完。这个入口不静态导入任何模块：
// 主体（js/core/runtime.js 与它导入的几十个模块）在入口执行时就开始在后台加载，不挡 Blazor 启动。
// 启动后同步做两件要赶在交互组件之前的事：注册自定义事件（渲染出 @onrunetoggle 这类处理器之前就要认得事件名）；
// 在 document 上为每种事件占好位置（下面的 SLOTS），主体到了再把处理函数挂进去。占位决定了处理顺序排在 Blazor 自己的事件委托之前：
// 有的处理依赖这一点（输入时先清掉旧的高亮，C# 渲染出新结果后再高亮第一项）。WebAssembly 下 C# 在事件里同步渲染，
// 真实的输入事件在两个监听器之间会跑一次微任务（DOM 变化的回调），顺序反了结果就错。

const runtime = import('./js/core/runtime.js');
let started = false;

// document 上的事件委托：[事件, 是否捕获]。主体要挂新的事件时在这里加上（没占位的会报错）
const SLOTS = [
  ['toggle', true], ['cancel', true], ['close', true], ['load', true], ['error', true],
  ['click', true], ['click', false], ['dblclick', false], ['auxclick', false], ['contextmenu', false],
  ['keydown', false], ['keyup', false], ['input', true], ['input', false], ['change', true], ['change', false],
  ['compositionend', false], ['paste', false], ['focusin', false], ['focusout', false], ['fullscreenchange', false],
  ['pointerdown', false], ['pointermove', false], ['pointerover', false], ['pointerout', false],
  ['dragenter', false], ['dragleave', false], ['drop', false],
];
const slots = new Map();

/** Blazor Web App 的初始化入口。 */
export function afterWebStarted(blazor) {
  start(blazor);
}

/** 独立的 Blazor Server / WebAssembly 应用的初始化入口。 */
export function afterStarted(blazor) {
  start(blazor);
}

function start(blazor) {
  if (started) return;
  started = true;
  registerEvents(blazor);
  for (const [type, capture] of SLOTS) slots.set(`${type} ${capture}`, listen(type, capture));
  runtime.then((module) => module.install(blazor, on));
}

/** 在 document 上占一个位置：之后挂上的处理函数按挂上的顺序执行，同原生的几个监听器 */
function listen(type, capture) {
  const handlers = [];
  document.addEventListener(type, (event) => {
    if (handlers.length === 0) return;
    // 调了 stopImmediatePropagation 的，后面的处理函数不再执行（输入法组合期间拦下 input）
    let stopped = false;
    const stop = event.stopImmediatePropagation;
    event.stopImmediatePropagation = () => {
      stopped = true;
      stop.call(event);
    };
    for (const handler of handlers) {
      // 一个出错不影响其他的（同各自独立的监听器）
      try {
        handler(event);
      } catch (error) {
        reportError(error);
      }
      if (stopped) break;
    }
  }, capture);
  return handlers;
}

/** 主体挂处理函数：document 上的这种事件（是否捕获） */
function on(type, handler, capture = false) {
  const handlers = slots.get(`${type} ${capture}`);
  if (!handlers) throw new Error(`Rune.UI：document 上的 ${type}（${capture ? '捕获' : '冒泡'}）没有占位，加到入口的 SLOTS 里`);
  handlers.push(handler);
}

/** Blazor 里的自定义事件（@onrunetoggle 等）：浏览器事件名与交给 C# 的参数 */
function registerEvents(blazor) {
  blazor.registerCustomEventType('runetoggle', {
    browserEventName: 'rune-toggle',
    createEventArgs: (event) => ({ open: event.detail.open, reason: event.detail.reason ?? null }),
  });
  blazor.registerCustomEventType('runecopied', {
    browserEventName: 'rune-copied',
    createEventArgs: (event) => ({ success: event.detail.success }),
  });
  blazor.registerCustomEventType('runedismiss', {
    browserEventName: 'rune-dismiss',
    createEventArgs: () => ({}),
  });
  blazor.registerCustomEventType('runecloserequest', {
    browserEventName: 'rune-close-request',
    createEventArgs: (event) => ({ reason: event.detail.reason }),
  });
  blazor.registerCustomEventType('runereorder', {
    browserEventName: 'rune-reorder',
    createEventArgs: (event) => ({ values: event.detail.values }),
  });
  blazor.registerCustomEventType('runeresize', {
    browserEventName: 'rune-resize',
    createEventArgs: (event) => ({ size: event.detail.size }),
  });
  blazor.registerCustomEventType('runeslide', {
    browserEventName: 'rune-slide',
    createEventArgs: (event) => ({ index: event.detail.index, value: event.detail.value, commit: event.detail.commit, seq: event.detail.seq }),
  });
  blazor.registerCustomEventType('runecommand', {
    browserEventName: 'rune-command',
    createEventArgs: (event) => ({ name: event.detail.name, text: event.detail.text ?? null }),
  });
}
