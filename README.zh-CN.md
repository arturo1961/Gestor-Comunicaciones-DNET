# Gestor de Comunicaciones DNET 1.6.1

[Español](README.md) | [简体中文](README.zh-CN.md)

Gestor de Comunicaciones DNET 是一款面向 Windows 的免费开源联系人和通信记录管理工具。它可以筛选联系人、把所选地址复制到密件抄送（BCC/CCO），或在默认邮件程序中创建一封新邮件。

程序**不会自动发送邮件**，不保存邮箱密码，也不会把私人主题或正文存入项目。每封邮件都由用户在自己的邮件程序中亲自检查、补充和发送。版本 1.6.1 在同一个可执行文件中提供 **62 种界面语言**，包括简体中文和繁体中文。

## 主要功能

- 新建、修改和受控删除联系人。
- 按国家、地区、语言、类型、组织和状态筛选。
- 按姓名、邮箱、地点、组织、状态或备注即时搜索。
- 选择收件人并把地址复制到密件抄送（BCC/CCO）。
- 通过 `mailto` 在默认邮件程序中打开新邮件。
- 手动记录并查看通信历史。
- 使用回收站恢复联系人或永久删除联系人。
- 查找可能重复的联系人和格式不正确的邮箱地址。
- 将联系人导入或导出为 JSON。
- 将联系人和历史记录完整备份为 ZIP。
- 恢复备份前，自动保存当前状态的安全副本。

## 数据与隐私

GitHub 仓库中包含的数据文件是空的。程序首次运行时，会把工作数据复制到：

`%LOCALAPPDATA%\Gestor_Comunicaciones_DNET\`

真实联系人和通信历史只保存在用户自己的电脑上。请勿公开该文件夹中的文件或程序生成的 ZIP 备份。

## 简明使用指南

1. 启动程序，使用“新建联系人”添加联系人，也可以导入 JSON 文件。
2. 在顶部语言菜单中选择简体中文；选择结果会自动保存。
3. 使用搜索框或筛选器找到需要的联系人。
4. 勾选收件人。使用“选择可见联系人”可选择当前列表中允许联系的全部人员。
5. 复制 BCC/CCO 地址，或通过 `mailto` 创建新邮件。
6. 在邮件程序中亲自填写主题和正文、添加附件，并在发送前检查密件抄送字段。
7. 实际发送后，返回管理工具并确认发送，以便写入历史记录。
8. 在“工具”中可以打开回收站、查看历史以及检查重复联系人或错误邮箱。
9. 使用“备份 > 创建备份”，把联系人和历史一起保存到 ZIP 文件。
10. 使用“恢复备份”返回以前的状态；替换数据前，程序会自动备份当前状态。

完整语言列表请参阅 [IDIOMAS.md](IDIOMAS.md)。自动生成的初始翻译可能需要改进，欢迎母语使用者提交修正。

## 下载与编译

已发布的 Windows 版本位于 [GitHub Releases 最新版本](https://github.com/arturo1961/Gestor-Comunicaciones-DNET/releases/latest)。

从源代码编译需要 Windows 10/11 和 .NET 8 SDK：

```powershell
dotnet build GestorComunicacionesDNET.csproj -c Release
```

生成 Windows x64 自包含单文件：

```powershell
dotnet publish GestorComunicacionesDNET.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o publicar
```

## 负责任地使用

群发邮件时始终使用密件抄送，尊重退订请求，并确保每次通信都有合法依据。本工具不会绕过邮件服务商的限制或政策。

## 免费软件与自愿捐赠

**Gestor de Comunicaciones DNET 现在免费，将来也会保持免费。是否捐赠完全由每位用户自行决定。** ❤️

- [PayPal](https://paypal.me/acarballoflandes)
- [Ko-fi](https://ko-fi.com/arturocarballo)
- Bizum：`+34 662 024 169`
- [GitHub Sponsors](https://github.com/sponsors/arturo1961)（个人赞助页面可用后）

捐赠不会解锁功能或带来特殊权限，也不会改变 MIT 许可证。你也可以通过报告问题、提出建议、改进翻译或分享项目来提供帮助。

## 许可证

本项目采用 [MIT 许可证](LICENSE)。允许在保留许可证和版权声明的前提下使用、研究、修改和分发源代码。
