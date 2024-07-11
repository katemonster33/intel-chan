using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace IntelChan.OpenAI
{
    public class OpenAIConfig
    {
        public enum MessageType
        {
            System,
            User,
            Assistant
        }
        public class SerializableChatMessage
        {
            public MessageType MessageType { get; set; }

            public string Message { get; set; }

            public SerializableChatMessage(MessageType messageType, string message)
            {
                MessageType = messageType;
                Message = message;
            }
        }
        public Dictionary<string, List<SerializableChatMessage>> ChatContexts { get; set; }

        public Dictionary<string, List<SerializableChatMessage>> SavedCharacters { get; set; }

        public OpenAIConfig()
        {
            ChatContexts = new Dictionary<string, List<SerializableChatMessage>>();
            SavedCharacters = new Dictionary<string, List<SerializableChatMessage>>();
        }

        public static OpenAIConfig LoadFromFile(string configFilePath)
        {
            return JsonSerializer.Deserialize<OpenAIConfig>(File.ReadAllText(configFilePath)) ?? new OpenAIConfig();
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this));
        }

        public bool TryGetContextMessages(string context, [NotNullWhen(true)] out List<ChatMessage>? messages)
        {
            if(ChatContexts.TryGetValue(context, out var serializableChats))
            {
                messages = new List<ChatMessage>();
                foreach(var msg in serializableChats)
                {
                    switch(msg.MessageType)
                    {
                        case MessageType.System:
                            messages.Add(new SystemChatMessage(msg.Message));
                            break;
                        case MessageType.User:
                            messages.Add(new UserChatMessage(msg.Message));
                            break;
                        case MessageType.Assistant:
                            messages.Add(new AssistantChatMessage(msg.Message));
                            break;
                    }
                }
                return true;
            }
            else
            {
                messages = null;
                return false;
            }
        }

        public List<string> GetCharacters()
        {
            return new List<string>(SavedCharacters.Keys);
        }

        public bool TryGetCharacterSummary(string characterName, [NotNullWhen(true)] out string? summary)
        {
            summary = null;
            if(SavedCharacters.TryGetValue(characterName, out var list))
            {
                summary = list[0].Message;
                return true;
            }
            return false;
        }

        public bool TryGetContextSummary(string context, [NotNullWhen(true)] out string? summary)
        {
            summary = null;
            if(ChatContexts.TryGetValue(context, out var list))
            {
                summary = list[0].Message ?? string.Empty;
                return true;
            }
            return false;
        }

        public bool StartCharacter(string context, string firstMessage)
        {
            if(ChatContexts.ContainsKey(context))
            {
                return false;
            }
            ChatContexts[context] = [ new SerializableChatMessage(MessageType.System, firstMessage )];
            return true;
        }

        public bool StopCharacter(string context)
        {
            return ChatContexts.Remove(context);
        }

        public bool SaveCharacter(string context, string characterName)
        {
            if(ChatContexts.TryGetValue(context, out var list))
            {
                SavedCharacters[characterName] = new List<SerializableChatMessage>(list);
                return true;
            }
            return false;
        }

        public bool LoadCharacter(string context, string characterName)
        {
            if(SavedCharacters.TryGetValue(characterName, out var list))
            {
                ChatContexts[context] = new List<SerializableChatMessage>(list);
                return true;
            }
            return false;
        }

        public bool Prune(string context, int num = -1)
        {
            if(ChatContexts.TryGetValue(context, out var list))
            {
                int numToDelete = (list.Count - 1) > num || num == -1 ? (list.Count - 1) : num;
                if(numToDelete > 0)
                {
                    list.RemoveRange(list.Count - numToDelete, numToDelete);
                    return true;
                }
            }
            return false;
        }
    }
}