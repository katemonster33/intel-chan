using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

namespace IntelChan.OpenAI
{
    public class OpenAIService
    {
        ChatClient OpenAiClient { get; }
        OpenAIConfig openAIConfig { get; }
        string configFilePath = "OpenAiCfg.json";

        public OpenAIService(IConfiguration config)
        {
            OpenAiClient = new ChatClient(config["openai-model"] ?? "gpt-4", config["openai-key"] ?? string.Empty, new());
            if(File.Exists(configFilePath))
            {
                openAIConfig = OpenAIConfig.LoadFromFile(configFilePath);
            }
            else openAIConfig = new OpenAIConfig();
        }

        public bool CompleteChat(string? context, string chat, [NotNullWhen(true)] out string? response)
        {
            if(context == null || !TryGetContextMessages(context, out var list))
            {
                list = new List<ChatMessage>();
            }
            list.Add(new UserChatMessage(chat));
            ChatCompletion comp = OpenAiClient.CompleteChat(list, new ChatCompletionOptions());
            if (comp != null)
            {
                response = comp.ToString();
                return true;
            }
            else
            {
                response = null;
                return false;
            }
        }


        public bool TryGetContextMessages(string context, [NotNullWhen(true)] out List<ChatMessage>? messages)
        {
            if(openAIConfig.ChatContexts.TryGetValue(context, out var serializableChats))
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

        public void Save()
        {
            openAIConfig.Save(configFilePath);
        }

        public List<string> GetCharacters()
        {
            return new List<string>(openAIConfig.SavedCharacters.Keys);
        }

        public bool TryGetCharacterSummary(string characterName, [NotNullWhen(true)] out string? summary)
        {
            summary = null;
            if(openAIConfig.SavedCharacters.TryGetValue(characterName, out var list))
            {
                summary = list[0].Message;
                return true;
            }
            return false;
        }

        public bool TryGetContextSummary(string context, [NotNullWhen(true)] out string? summary)
        {
            summary = null;
            if(openAIConfig.ChatContexts.TryGetValue(context, out var list))
            {
                summary = list[0].Message ?? string.Empty;
                return true;
            }
            return false;
        }

        public bool StartCharacter(string context, string firstMessage)
        {
            if(openAIConfig.ChatContexts.ContainsKey(context))
            {
                return false;
            }
            openAIConfig.ChatContexts[context] = [ new SerializableChatMessage(MessageType.System, firstMessage )];
            Save();
            return true;
        }

        public bool StopCharacter(string context)
        {
            Save();
            return openAIConfig.ChatContexts.Remove(context);
        }

        public bool SaveCharacter(string context, string characterName)
        {
            if(openAIConfig.ChatContexts.TryGetValue(context, out var list))
            {
                openAIConfig.SavedCharacters[characterName] = new List<SerializableChatMessage>(list);
                Save();
                return true;
            }
            return false;
        }

        public bool LoadCharacter(string context, string characterName)
        {
            if(openAIConfig.SavedCharacters.TryGetValue(characterName, out var list))
            {
                openAIConfig.ChatContexts[context] = new List<SerializableChatMessage>(list);
                Save();
                return true;
            }
            return false;
        }

        public bool Prune(string context, int num = -1)
        {
            if(openAIConfig.ChatContexts.TryGetValue(context, out var list))
            {
                int numToDelete = (list.Count - 1) > num || num == -1 ? (list.Count - 1) : num;
                if(numToDelete > 0)
                {
                    list.RemoveRange(list.Count - numToDelete, numToDelete);
                    Save();
                    return true;
                }
            }
            return false;
        }
    }
}