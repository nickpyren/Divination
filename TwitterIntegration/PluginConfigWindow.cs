using System.Diagnostics;
using System.Linq;
using CoreTweet;
using Dalamud.Divination.Common.Api.Dalamud;
using Dalamud.Divination.Common.Api.Ui;
using Dalamud.Divination.Common.Api.Ui.Window;
using Divination.TwitterIntegration.Credentials;
using ImGuiNET;

namespace Divination.TwitterIntegration;

public class PluginConfigWindow : ConfigWindow<PluginConfig>
{
    private static string _consumerKeyInput = string.Empty;
    private static string _consumerSecretInput = string.Empty;
    private static string _accessTokenInput = string.Empty;
    private static string _accessTokenSecretInput = string.Empty;

    public override void Draw()
    {
        if (ImGui.Begin($"{TwitterIntegration.Instance.Name} 設定", ref IsOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
        {
            DrawCredentialInput("Consumer Key", ref _consumerKeyInput);
            DrawCredentialInput("Consumer Secret", ref _consumerSecretInput);
            DrawCredentialInput("Access Token", ref _accessTokenInput);
            DrawCredentialInput("Access Token Secret", ref _accessTokenSecretInput);
            ImGui.TextDisabled("Saved credentials are not displayed. Leave a field empty to keep its current value.");

            ImGui.Separator();

            ImGuiEx.CheckboxConfig("Show list TL", ref Config.ShowListTimeline);
            ImGuiEx.TextConfig("List ID", ref Config.ListId, 32);
            ImGui.InputInt("Update Interval (ms)", ref Config.UpdateIntervalInMs);

            ImGui.Separator();

            CreateAuthenticateButton();
            ImGui.SameLine();
            CreateFindListButton();
            ImGui.SameLine();
            CreateClearCredentialsButton();

            ImGui.Separator();

            if (ImGui.Button("Save & Close"))
            {
                SaveCredentialInputs();
                IsOpen = false;

                TwitterIntegration.Instance.Dalamud.PluginInterface.SavePluginConfig(Config);
                DalamudLog.Log.Information("Config saved");
            }

            ImGui.End();
        }

        CreatePinWindow();
    }

    private static void DrawCredentialInput(string label, ref string value)
    {
        ImGui.InputText(label, ref value, 256, ImGuiInputTextFlags.Password);
    }

    private static void SaveCredentialInputs()
    {
        WriteIfPresent(TwitterCredentialKeys.ConsumerKey, ref _consumerKeyInput);
        WriteIfPresent(TwitterCredentialKeys.ConsumerSecret, ref _consumerSecretInput);
        WriteIfPresent(TwitterCredentialKeys.AccessToken, ref _accessTokenInput);
        WriteIfPresent(TwitterCredentialKeys.AccessTokenSecret, ref _accessTokenSecretInput);
    }

    private static void WriteIfPresent(string key, ref string input)
    {
        if (!string.IsNullOrWhiteSpace(input))
        {
            TwitterIntegration.Instance.WriteCredential(key, input);
        }

        input = string.Empty;
    }

    private static OAuth.OAuthSession? _session;
    private static bool _isPinWindowDrawing;
    private static string _pin = string.Empty;

    private static void CreateAuthenticateButton()
    {
        if (ImGui.Button("Authenticate"))
        {
            SaveCredentialInputs();

            var consumerKey = TwitterIntegration.Instance.ReadCredential(TwitterCredentialKeys.ConsumerKey);
            var consumerSecret = TwitterIntegration.Instance.ReadCredential(TwitterCredentialKeys.ConsumerSecret);
            if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret))
            {
                TwitterIntegration.Instance.Divination.Chat.PrintError("Consumer Key または Consumer Secret が設定されていません。");
                return;
            }

            _session = OAuth.Authorize(consumerKey, consumerSecret);
            Process.Start(_session!.AuthorizeUri.AbsoluteUri);

            _isPinWindowDrawing = true;
        }
    }

    private static void CreatePinWindow()
    {
        if (_isPinWindowDrawing && _pin.Length == 7)
        {
            var tokens = _session?.GetTokens(_pin);
            if (tokens != null)
            {
                TwitterIntegration.Instance.WriteCredential(TwitterCredentialKeys.AccessToken, tokens.AccessToken);
                TwitterIntegration.Instance.WriteCredential(TwitterCredentialKeys.AccessTokenSecret, tokens.AccessTokenSecret);

                TwitterIntegration.Instance.Divination.Chat.Print("Twitter API への認証に成功しました。");
            }

            _session = null;
            _isPinWindowDrawing = false;
            _pin = string.Empty;
        }

        if (!_isPinWindowDrawing)
        {
            return;
        }

        if (ImGui.Begin("PinWindow",
            ref _isPinWindowDrawing,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.Text("Enter PIN code:");

            ImGui.InputText("", ref _pin, 7, ImGuiInputTextFlags.Password);
            ImGui.End();
        }
    }

    private static void CreateFindListButton()
    {
        if (ImGui.Button("Find List"))
        {
            if (TwitterIntegration.Twitter == null)
            {
                TwitterIntegration.Instance.Divination.Chat.PrintError("Twitter API の資格情報が設定されていません。");
                return;
            }

            TwitterIntegration.Twitter.Lists.OwnershipsAsync(count: 50)
                .ContinueWith(completed =>
                {
                    if (completed.IsCompleted)
                    {
                        TwitterIntegration.Instance.Divination.Chat.Print(
                            $"使用可能なリスト一覧です。\n{string.Join("\n", completed.Result.Select(list => $"{list.Name} (ID: {list.Id})"))}");
                    }
                    else if (completed.Exception != null)
                    {
                        TwitterIntegration.Instance.Divination.Chat.PrintError("リスト一覧の取得に失敗しました。");
                        DalamudLog.Log.Error(completed.Exception, "Error occurred while OwnershipsAsync");
                    }
                });
        }
    }

    private static void CreateClearCredentialsButton()
    {
        if (!ImGui.Button("Clear stored credentials"))
        {
            return;
        }

        TwitterIntegration.Instance.DeleteCredential(TwitterCredentialKeys.ConsumerKey);
        TwitterIntegration.Instance.DeleteCredential(TwitterCredentialKeys.ConsumerSecret);
        TwitterIntegration.Instance.DeleteCredential(TwitterCredentialKeys.AccessToken);
        TwitterIntegration.Instance.DeleteCredential(TwitterCredentialKeys.AccessTokenSecret);
        TwitterIntegration.Instance.Divination.Chat.Print("Stored Twitter credentials were cleared.");
    }
}
