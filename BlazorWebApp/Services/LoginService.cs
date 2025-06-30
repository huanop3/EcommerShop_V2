using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using web_api_base.Models.ViewModel;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorWebApp.Services
{
    public class LoginService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authStateProvider;

        public LoginService(
            HttpClient httpClient,
            ILocalStorageService localStorage,
            IJSRuntime jsRuntime,
            NavigationManager navigationManager,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
            _authStateProvider = authStateProvider;
        }
        // Gán token vào header trước khi gọi API
        private async Task SetAuthorizationHeader()
        {
            var token = await _localStorage.GetItemAsStringAsync("token");
            if (string.IsNullOrEmpty(token))
            {
                // Nếu không có token, thử refresh
                token = await _localStorage.GetItemAsStringAsync("refreshToken");
            }

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        /// <summary>
        /// Kiểm tra trạng thái đăng nhập của người dùng
        /// </summary>
        public async Task<bool> CheckAuthenticationStatus()
        {
            try
            {
                // Lấy token từ local storage
                var token = await _localStorage.GetItemAsStringAsync("refreshToken");

                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // Bỏ dấu ngoặc kép nếu có
                if (token.StartsWith("\"") && token.EndsWith("\""))
                {
                    token = token.Trim('"');
                    // Lưu lại token đã sửa
                    await _localStorage.SetItemAsStringAsync("refreshToken", token);
                }

                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                {
                    Console.WriteLine("Token không đúng định dạng JWT");
                    await _localStorage.RemoveItemAsync("token");
                    await _localStorage.RemoveItemAsync("refreshToken");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi xác thực: {ex.Message}");
                await _localStorage.RemoveItemAsync("token");

                await _localStorage.RemoveItemAsync("refreshToken");
                return false;
            }
        }
        /// <summary>
        /// Lấy thông tin user từ token
        /// </summary>
        public async Task<string> GetUserName()
        {
            try
            {
                var token = await _localStorage.GetItemAsStringAsync("token");
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jsonToken != null)
                {
                    var userName = jsonToken.Claims.First(claim => claim.Type == "unique_name").Value;
                    return userName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy tên người dùng: {ex.Message}");
            }
            return null;
        }
        /// <summary>
        /// Đăng nhập vào hệ thống
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> Login(UserLoginVM loginModel)
        {
            try
            {
                // Thu thập thông tin thiết bị từ JavaScript thông qua phương thức GetDeviceInfo
                var deviceInfo = await GetDeviceInfo();
                if (deviceInfo == null)
                {
                    return (false, "Không thể thu thập thông tin thiết bị. Vui lòng bật JavaScript và thử lại.");
                }

                // Tạo LoginRequestVM với thông tin đăng nhập và thông tin thiết bị
                var loginRequest = new LoginRequestVM
                {
                    Username = loginModel.Username,
                    Password = loginModel.Password,
                    DeviceID = deviceInfo.DeviceID,
                    DeviceName = deviceInfo.DeviceName,
                    DeviceOS = deviceInfo.DeviceOS,
                    ClientName = deviceInfo.ClientName,
                    IPAddress = deviceInfo.IPAddress,
                    CollectedAt = DateTime.Now
                };

                // Gọi API đăng nhập
                var response = await _httpClient.PostAsJsonAsync("main/api/UserLogin/LoginUser", loginRequest);

                // Phân tích phản hồi từ server
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<UserLoginResponseVM>>();

                if (result == null)
                {
                    return (false, "Không nhận được phản hồi từ server");
                }

                // Nếu đăng nhập thành công
                if (result.Success && result.Data != null)
                {
                    // Lưu token vào local storage
                    await _localStorage.SetItemAsStringAsync("token", result.Data.AccessToken);


                    // Lưu refresh token
                    if (!string.IsNullOrEmpty(result.Data.RefreshToken))
                    {
                        await _localStorage.SetItemAsStringAsync("refreshToken", result.Data.RefreshToken);
                    }

                    // Lưu token vào cookie với thời hạn ngắn (1 ngày)
                    await _jsRuntime.InvokeVoidAsync("setCookie", "token", result.Data.AccessToken, 1);

                    // Thông báo đăng nhập thành công

                    return (true, result.Message ?? "Đăng nhập thành công");
                }
                else
                {
                    // Trả về thông báo lỗi từ server
                    return (false, result.Message ?? "Đăng nhập không thành công");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đăng nhập: {ex.Message}");
                return (false, "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại sau.");
            }
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> Register(RegisterLoginVM registerModel)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("main/api/UserLogin/RegisterUser", registerModel);

                // Đọc phản hồi từ server
                var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<RegisterLoginVM>>();

                if (result == null)
                {
                    return (false, "Không nhận được phản hồi từ server");
                }

                if (result.Success)
                {
                    return (true, result.Message ?? "Đăng ký thành công");
                }
                else
                {
                    return (false, result.Message ?? "Đăng ký không thành công");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đăng ký: {ex.Message}");
                return (false, "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại sau.");
            }
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        public async Task Logout()
        {
            await SetAuthorizationHeader();
            var refreshToken = await _localStorage.GetItemAsStringAsync("refreshToken");

            // Lưu lại token đã sửa
            var response = await _httpClient.PutAsJsonAsync("main/api/UserLogin/Logout", refreshToken);
            if (response.IsSuccessStatusCode)
            {
                await _localStorage.RemoveItemAsync("token");
                await _localStorage.RemoveItemAsync("refreshToken");
                await _jsRuntime.InvokeVoidAsync("deleteCookie", "token");

                // Thông báo đã đăng xuất
            }
            else
            {
                Console.WriteLine("Lỗi khi đăng xuất");
            }


        }

        public async Task<DeviceInfoVM> CollectDeviceInfo()
        {
            try
            {
                // Kiểm tra xem đã có thông tin thiết bị chưa
                var existingInfo = await _localStorage.GetItemAsync<DeviceInfoVM>("deviceInfo");
                if (existingInfo != null)
                {
                    // Cập nhật thời gian thu thập
                    existingInfo.CollectedAt = DateTime.Now;
                    await _localStorage.SetItemAsync("deviceInfo", existingInfo);
                    return existingInfo;
                }

                // Thiết lập timeout cho JavaScript interop
                var timeoutTask = Task.Delay(5000); // 5 giây timeout

                // Thu thập thông tin thiết bị từ JavaScript
                var jsTask = _jsRuntime.InvokeAsync<DeviceInfoVM>("getFullDeviceInfo").AsTask();

                // Chờ task nào hoàn thành trước
                var completedTask = await Task.WhenAny(jsTask, timeoutTask);

                // Nếu JavaScript interop hoàn thành trước timeout
                if (completedTask == jsTask && !jsTask.IsFaulted && !jsTask.IsCanceled)
                {
                    var deviceInfo = await jsTask;
                    if (deviceInfo != null)
                    {
                        deviceInfo.CollectedAt = DateTime.Now;
                        // Lưu vào localStorage
                        await _localStorage.SetItemAsync("deviceInfo", deviceInfo);
                        return deviceInfo;
                    }
                }

                // Nếu JavaScript interop bị lỗi hoặc timeout, tạo thông tin mặc định
                Console.WriteLine("Không thể thu thập thông tin thiết bị từ JavaScript - sử dụng thông tin mặc định");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thu thập thông tin thiết bị: {ex.Message}");
                return null;
            }
        }
        public async Task<DeviceInfoVM> GetDeviceInfo()
        {
            try
            {
                var deviceInfo = await _localStorage.GetItemAsync<DeviceInfoVM>("deviceInfo");
                if (deviceInfo == null)
                {
                    deviceInfo = await CollectDeviceInfo();
                }
                return deviceInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy thông tin thiết bị: {ex.Message}");
                return null;
            }
        }
        public async Task<(string, bool)> ForgotPassword(ForgotPasswordVM forgotPasswordModel)
        {
            // Gọi API để gửi email quên mật khẩu
            var response = await _httpClient.PostAsJsonAsync("main/api/UserLogin/forgot", forgotPasswordModel);

            // Đọc phản hồi từ server
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<ForgotPasswordResponseVM>>();

            if (result == null)
            {
                return (null, false);
            }

            return (result.Data.Token, result.Data.IsSuccess);
        }
        /// <summary>
        /// </summary>
        public async Task<bool> ResetPassword(ResetPasswordVM resetPasswordModel)
        {
            // Gọi API để đặt lại mật khẩu
            var response = await _httpClient.PostAsJsonAsync("main/api/UserLogin/reset", resetPasswordModel);

            // Đọc phản hồi từ server
            var result = await response.Content.ReadFromJsonAsync<HTTPResponseClient<ResetPasswordResponseVM>>();

            if (result == null)
            {
                return false;
            }

            return result.Data.IsSuccess;
        }

    }
}