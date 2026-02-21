namespace Cross.Notification.Telegram.Enum;

public enum TelegramChannelTypeEnum
{
    Public = 0,
    Private = 1,
    Group = 2,
    Supergroup = 3,
    Bot = 4
}


// Отличный вопрос 👍
// Давай разберём по каждому мессенджеру отдельно, потому что у них принципиально разные модели каналов и чатов.
//
// ⸻
//
// 🟦 Telegram
//
// Telegram имеет унифицированную модель чатов, но логически выделяются следующие типы:
//
// Тип	Enum	Описание
// User	User = 0	Приватный диалог (one-to-one, direct chat). Это не бот и не группа.
// Bot	Bot = 1	Аккаунт с типом bot, с которым можно взаимодействовать через API.
// Group	Group = 2	Обычная группа до 200 участников. (старый формат)
// Supergroup	Supergroup = 3	Расширенная группа (до 200 000 участников, поддержка админов, join-requests и т.п.).
// ChannelPublic	ChannelPublic = 4	Публичный канал (t.me/<username>), можно подписаться.
// ChannelPrivate	ChannelPrivate = 5	Приватный канал без публичного username (t.me/joinchat/...).
//
// ⚙️ Внутри Telegram API (Chat.Type) это строки: "private", "group", "supergroup", "channel".
// "bot" не отдельный тип, а флаг User.IsBot = true.
//
// 💡 То есть твоё перечисление можно уточнить вот так:
//
// public enum TelegramChatTypeEnum
// {
//     Private = 0,      // личный чат (включая бот)
//     Group = 1,        // группа
//     Supergroup = 2,   // супергруппа
//     Channel = 3,      // канал (публичный или приватный)
// }
//
//
// ⸻
//
// 🟣 Viber
//
// Viber разделяет Personal, Group, Community и Bot.
// В официальном API есть только два типа для ботов — но в реальности больше:
//
// Тип	Enum	Описание
// PrivateChat	Private = 0	Приватный чат с пользователем.
// Group	Group = 1	Групповой чат (несколько пользователей).
// Community	Community = 2	Расширенная группа с открытыми ссылками, администраторами, каналами.
// PublicAccount	PublicAccount = 3	Старое название для публичного профиля (до Community).
// Bot	Bot = 4	Бот-аккаунт, работающий через Viber Bot API.
//
// В Viber REST API (callbacks) есть поле "chat_type": "private" | "group", но Community и PublicAccount встречаются в управлении каналами.
//
// ⸻
//
// 🟩 WhatsApp
//
// У WhatsApp модель проще, но за последние годы добавились каналы.
//
// Тип	Enum	Описание
// Individual	Individual = 0	Личный чат (1:1)
// Group	Group = 1	Групповой чат (до 1024 участников)
// Community	Community = 2	Новая структура, объединяющая несколько групп
// Channel	Channel = 3	Односторонний канал (аналог Telegram Channel, с 2023 года)
// Business	Business = 4	Бизнес-аккаунт (через WhatsApp Business API)
//
// ⚙️ В WhatsApp Cloud API возвращается "type": "individual" | "group",
// а каналы и сообщества пока не полностью отражены в публичном API (Meta их вводит постепенно).
//
// ⸻
//
// ✅ Итого — обобщённый набор
//
// Если тебе нужен единый enum для всей системы (например, в Cross.Notification),
// можно сделать универсальное:
//
// public enum ChatChannelTypeEnum
// {
//     Private = 0,      // 1:1, Direct, DM
//     Group = 1,        // Group / Supergroup / Community
//     Channel = 2,      // One-way broadcast (Telegram, WhatsApp)
//     Bot = 3,          // Bot channel (Telegram/Viber)
//     Business = 4,     // Business API chat (WhatsApp Business)
// }
//
//
// ⸻
//
// Хочешь — я сделаю сводную таблицу соответствий:
// TelegramChatTypeEnum, ViberChatTypeEnum, WhatsAppChatTypeEnum → ChatChannelTypeEnum
// (удобно для нормализации сообщений между платформами).
