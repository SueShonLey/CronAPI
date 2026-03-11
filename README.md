# 轻量级接口定时调度工具

一款基于 SQLite 轻量级存储的定时接口调度工具，专注于接口调度任务的全生命周期管理（创建、配置、执行、管理），轻量化部署、易上手、易扩展。

## ✨ 核心特性
- **全生命周期管理**：支持接口调度任务的创建、配置、执行、暂停、删除等完整生命周期操作
- **多触发方式**：覆盖四类核心触发场景，满足多样化调度需求：
  - 定时触发：基于 Cron 表达式精准定时执行
  - 手动触发：支持人为手动点击立即执行
  - 排队触发：定时触发时若接口处于调用中，自动将计划入队等待执行
  - 错误重试：定时/排队触发出错时，按预设重试次数自动重试
- **多调用模式**：可根据业务场景灵活选择执行策略：
  - 排队模式：任务正在执行则等待执行结束后再触发（需注意调用间隔，避免队列堆积）
  - 并发模式：无视任务执行状态，立即触发执行
  - 跳过模式：任务正在执行则直接放弃本次触发
- **轻量级部署**：基于 SQLite 存储，无需额外部署数据库，开箱即用
- **队列可控**：支持队列可视化管理，可手动清空队列、调整调用频率

## 🛠 技术栈
本工具基于优秀开源组件构建，感谢以下项目的贡献：
- 数据访问：FreeSql
- 定时任务：Quartz
- UI 组件：WinFormLib
- 存储引擎：SQLite

## ⚠ 注意事项
1. 排队模式下，接口两次调用间隔建议不小于单次调用耗时，否则可能导致队列堆积，可通过队列管理界面实时调整调用频率；
2. 排队模式下，禁用调度计划后仍会消费已入队的任务计划，需手动清空队列方可完全停止执行。

## 📄 开源声明
本项目基于MIT开源协议开发，感谢开源社区的所有贡献者！

## 📞 联系与反馈
如有问题/建议，可通过 Issues 反馈, 谢谢。

## 作品预览
<img width="1404" height="855" alt="image" src="https://github.com/user-attachments/assets/3b387cdd-b181-460b-9e35-032c5c03a4b6" />

<img width="1420" height="863" alt="image" src="https://github.com/user-attachments/assets/b0ccbe35-a6a9-4c97-a835-084e11da9435" />

<img width="1420" height="863" alt="image" src="https://github.com/user-attachments/assets/bd1c7706-20e1-4b8f-80e1-4c5e088ccd31" />

<img width="1420" height="863" alt="image" src="https://github.com/user-attachments/assets/05fa107d-89a3-42e3-bbdc-f8be5b6913f6" />

<img width="1420" height="863" alt="image" src="https://github.com/user-attachments/assets/fdf2c1f0-a80b-49a8-9ce7-2f3b3b076674" />

<img width="1462" height="2640" alt="tips" src="https://github.com/user-attachments/assets/bcab6061-d888-42e2-9004-55a647e38c4d" />

