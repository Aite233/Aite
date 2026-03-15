using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Linq;
using Aite.Core.Utils;
using Aite.Config.Utils;
namespace Aite.WPF;

public partial class ActivationWindow : Window
{
    public bool IsActivated { get; private set; } = false;
    
    public ActivationWindow()
    {
        InitializeComponent();
        
        string lastKey = LoadLastKey();
        if (!string.IsNullOrEmpty(lastKey))
        {
            ActivationCodeTextBox.Text = lastKey;
        }
    }
    
    private string LoadLastKey()
    {
        try
        {
            string keyFile = Path.Combine(PathUtil.CachePath, "last_key.dat");
            if (File.Exists(keyFile))
            {
                return File.ReadAllText(keyFile);
            }
        }
        catch (Exception)
        {
        }
        return string.Empty;
    }
    
    private void SaveLastKey(string activationCode)
    {
        try
        {
            string keyFile = Path.Combine(PathUtil.CachePath, "last_key.dat");
            File.WriteAllText(keyFile, activationCode);
        }
        catch (Exception)
        {
        }
    }
    
    private byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException("Hex string must have even length");
        }
        
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
    
    private bool ValidateActivationCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }
        
        foreach (char c in code)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
    
    private async void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        string activationCode = ActivationCodeTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(activationCode))
        {
            ErrorTextBlock.Text = "请输入激活码";
            return;
        }
        
        if (ValidateActivationCode(activationCode))
        {
            SaveLastKey(activationCode);
            ErrorTextBlock.Text = "正在验证...";
            var (success, response, crcSalt) = await SendValidationMessage(activationCode);
            
            string displayMessage = response;
            try
            {
                var jsonDocument = JsonDocument.Parse(response);
                var jsonData = jsonDocument.RootElement;
                if (jsonData.TryGetProperty("msg", out System.Text.Json.JsonElement msgProperty) && msgProperty.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    displayMessage = msgProperty.GetString();
                }
            }
            catch { }
            ErrorTextBlock.Text = "验证完成: " + displayMessage;
            
            if (success && !string.IsNullOrEmpty(crcSalt))
            {
                IsActivated = true;
                WPFLauncherApi.Protocol.X19.CrcSalt = crcSalt;
                Close();
            }
            else
            {
                IsActivated = false;
                string errorDisplayMessage = response;
                try
                {
                    if (response.StartsWith("错误: "))
                    {
                        errorDisplayMessage = response.Substring(4);
                    }
                    else
                    {
                        var jsonDocument = JsonDocument.Parse(response);
                        var jsonData = jsonDocument.RootElement;
                        if (jsonData.TryGetProperty("msg", out System.Text.Json.JsonElement msgProperty) && msgProperty.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            errorDisplayMessage = msgProperty.GetString();
                        }
                    }
                }
                catch { }
                ErrorTextBlock.Text = "验证失败: " + errorDisplayMessage;
            }
        }
        else
        {
            ErrorTextBlock.Text = "无效的激活码，请重新输入";
        }
    }
    
    private async Task<(bool, string, string)> SendValidationMessage(string activationCode)
    {
        try
        {
            string cpuSerial = HardwareInfo.GetCpuSerialNumber();
            
            string hwid = MD5Util.GetMD5Hash("AiteCode" + cpuSerial);
            
            var messageObject = new { hwid = hwid, key = activationCode };
            string message = JsonSerializer.Serialize(messageObject);
            
            RSACipher rsa = new RSACipher();
            string encryptedMessage = rsa.Encrypt(message);
            
            string encryptedJson = encryptedMessage;
            
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://103.40.13.4:12345");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));
                
                var content = new StringContent(encryptedJson, Encoding.UTF8, "text/plain");
                HttpResponseMessage response = await client.PostAsync("/verify", content);
                
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    RC4Cipher rc4 = new RC4Cipher();
                    byte[] responseBytes = HexToBytes(responseContent);
                    string decryptedContent = rc4.Decrypt(responseBytes);
                    
                    try
                    {
                        var jsonDocument = JsonDocument.Parse(decryptedContent);
                        var jsonData = jsonDocument.RootElement;
                        
                        if (jsonData.ValueKind == JsonValueKind.Null)
                        {
                            string errorMessage = "解析响应数据失败，数据为空";
                            return (false, errorMessage, "");
                        }

                        if(jsonData.TryGetProperty("hwid", out System.Text.Json.JsonElement hwidProperty) && hwidProperty.ValueKind != System.Text.Json.JsonValueKind.Null && hwidProperty.GetString() != hwid)
                        {
                            string errorMessage = "HWID不匹配";
                            return (false, errorMessage, "");
                        }
                        
                        bool crcNormal = false;
                        string crcSalt = "";
                        if (jsonData.TryGetProperty("crc", out System.Text.Json.JsonElement crcProperty) && crcProperty.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            if (crcProperty.TryGetProperty("code", out System.Text.Json.JsonElement codeProperty))
                            {
                                if (codeProperty.GetInt32() == 1)
                                {
                                    crcNormal = true;
                                    if (crcProperty.TryGetProperty("crcSalt", out System.Text.Json.JsonElement crcSaltProperty) && crcSaltProperty.ValueKind != System.Text.Json.JsonValueKind.Null)
                                    {
                                        crcSalt = crcSaltProperty.GetString();
                                    }
                                }
                            }
                        }
                        
                        if (crcNormal && !string.IsNullOrEmpty(crcSalt))
                        {
                            return (true, decryptedContent, crcSalt);
                        }
                        else
                        {
                            string errorMsg = "未知错误";
                            if (jsonData.TryGetProperty("msg", out System.Text.Json.JsonElement msgProperty) && msgProperty.ValueKind != System.Text.Json.JsonValueKind.Null)
                            {
                                string rawMsg = msgProperty.GetString();
                                if (!string.IsNullOrEmpty(rawMsg))
                                {
                                    errorMsg = rawMsg;
                                }
                            }
                            else if (jsonData.TryGetProperty("crc", out System.Text.Json.JsonElement crcObj) && crcObj.ValueKind != System.Text.Json.JsonValueKind.Null)
                            {
                                if (crcObj.TryGetProperty("msg", out System.Text.Json.JsonElement crcMsgProperty) && crcMsgProperty.ValueKind != System.Text.Json.JsonValueKind.Null)
                                {
                                    string rawMsg = crcMsgProperty.GetString();
                                    if (!string.IsNullOrEmpty(rawMsg))
                                    {
                                        errorMsg = rawMsg;
                                    }
                                }
                            }
                            return (false, errorMsg, "");
                        }
                    }
                    catch (Exception ex)
                    {
                        string cleanMessage = new string(ex.Message.Where(c => c < 128).ToArray());
                        string errorMessage = $"解析响应数据时出现异常: {cleanMessage}";
                        return (false, errorMessage, "");
                    }
                }
                else
                {
                    string errorMessage = $"HTTP请求失败，状态码: {response.StatusCode}";
                    return (false, errorMessage, "");
                }
            }
        }
        catch (Exception ex)
        {
            string errorMessage = $"发送验证消息时出现异常: {ex.Message}";
            return (false, errorMessage, "");
        }
    }
}
