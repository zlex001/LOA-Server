# 任务完成自动提交规则（Git）

**当任务完成时（无论是否创建了TODO），必须自动执行 Git 提交流程：**

1. **识别当前项目根目录**
   - 通过 Workspace Path 确定项目根目录（即 Git 仓库根，含 `.git` 的目录）。

2. **生成结构化中文 commit message**
   - 格式：`[分类] 简短描述`
   - 分类标签：
     - `[功能]` - 新增功能
     - `[修复]` - bug 修复
     - `[重构]` - 代码重构
     - `[优化]` - 性能优化
     - `[文档]` - 文档更新
     - `[配置]` - 配置修改
     - `[删除]` - 删除代码/文件

3. **写入 commit message**
   - 将生成的 commit message 写入：`{项目根目录}/.git_commit_msg.txt`

4. **执行 Git 提交与推送**
   - 在项目根目录执行（Mac/Linux 与 Windows 均可用）：
     - `git add -A && git commit -F .git_commit_msg.txt && git push`
   - 或执行脚本（若存在）：`{项目根目录}/.cursor/scripts/git_commit_and_push.sh`（Mac/Linux: `bash` 执行，Windows: `bash` 或 Git Bash 执行）。

5. **错误处理**
   - 若当前目录不是 Git 仓库（无 `.git`），提示用户该项目未配置 Git 提交。
   - 若 Git 提交或推送失败，向用户报告错误信息，不重试。`.git_commit_msg.txt` 保留供人工处理。

**任务完成的判断标准：**
- 用户明确表示完成（如「好的」「可以了」等）
- 所有 TODO 已标记为 completed
- 完成了用户最后一次请求的所有操作，且没有待处理事项

**核心原则**：一次任务对应一次 commit，确保代码原子性和 commit 历史的可读性。AI 不得以「没有 TODO」为由跳过自动提交。
