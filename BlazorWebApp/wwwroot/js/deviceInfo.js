// Khởi tạo ID thiết bị ngẫu nhiên nếu chưa có
function generateDeviceId() {
    let deviceId = localStorage.getItem('blazor_deviceId');
    if (!deviceId) {
        deviceId = 'device_' + Math.random().toString(36).substring(2, 15) + 
                   Math.random().toString(36).substring(2, 15);
        localStorage.setItem('blazor_deviceId', deviceId);
    }
    return deviceId;
}

// Thu thập thông tin thiết bị
function getDeviceInfo() {
    const userAgent = navigator.userAgent;
    const platform = navigator.platform;
    
    // Xác định tên trình duyệt
    let clientName = "Unknown Browser";
    if (userAgent.indexOf("Firefox") > -1) {
        clientName = "Firefox";
    } else if (userAgent.indexOf("Opera") > -1 || userAgent.indexOf("OPR") > -1) {
        clientName = "Opera";
    } else if (userAgent.indexOf("Trident") > -1) {
        clientName = "Internet Explorer";
    } else if (userAgent.indexOf("Edge") > -1 || userAgent.indexOf("Edg") > -1) {
        clientName = "Microsoft Edge";
    } else if (userAgent.indexOf("Chrome") > -1) {
        clientName = "Chrome";
    } else if (userAgent.indexOf("coc_coc_browser") > -1 || userAgent.indexOf("CocCoc") > -1) {
    clientName = "Cốc Cốc";
    } else if (userAgent.indexOf("Safari") > -1) {
        clientName = "Safari";
    } 
    // Xác định hệ điều hành
    let deviceOS = "Unknown OS";
    if (/Windows NT 10.0/i.test(userAgent)) {
        deviceOS = "Windows 10";
    } else if (/Windows NT 6.3/i.test(userAgent)) {
        deviceOS = "Windows 8.1";
    } else if (/Windows NT 6.2/i.test(userAgent)) {
        deviceOS = "Windows 8";
    } else if (/Windows NT 6.1/i.test(userAgent)) {
        deviceOS = "Windows 7";
    } else if (/Windows NT 6.0/i.test(userAgent)) {
        deviceOS = "Windows Vista";
    } else if (/Windows NT 5.1/i.test(userAgent)) {
        deviceOS = "Windows XP";
    } else if (/Mac OS X/i.test(userAgent)) {
        deviceOS = "Mac OS X";
    } else if (/Android/i.test(userAgent)) {
        deviceOS = "Android";
    } else if (/iOS|iPhone|iPad|iPod/i.test(userAgent)) {
        deviceOS = "iOS";
    } else if (/Linux/i.test(userAgent)) {
        deviceOS = "Linux";
    }
    
    // Đoán tên thiết bị
    let deviceName = "Unknown Device";
    if (/iPhone/i.test(userAgent)) {
        deviceName = "iPhone";
    } else if (/iPad/i.test(userAgent)) {
        deviceName = "iPad";
    } else if (/Android/i.test(userAgent)) {
        const brand = userAgent.match(/Android.*; (.*?) Build\//i);
        if (brand && brand.length > 1) {
            deviceName = brand[1];
        } else {
            deviceName = "Android Device";
        }
    } else if (/Mac/i.test(platform)) {
        deviceName = "Mac";
    } else if (/Win/i.test(platform)) {
        deviceName = "Windows PC";
    } else if (/Linux/i.test(platform)) {
        deviceName = "Linux PC";
    }
    
    return {
        clientName: clientName,
        deviceID: generateDeviceId(),
        deviceName: deviceName,
        deviceOS: deviceOS
    };
}

// Lấy IP địa chỉ từ dịch vụ bên ngoài 
async function getIPAddress() {
    try {
        const response = await fetch('https://api.ipify.org?format=json');
        const data = await response.json();
        return data.ip;
    } catch (error) {
        console.error('Error fetching IP address:', error);
        return 'Unknown';
    }
}

// Lấy tất cả thông tin thiết bị kết hợp với IP
window.getFullDeviceInfo = async function() {
    const deviceInfo = getDeviceInfo();
    deviceInfo.IPAddress = await getIPAddress();
    return deviceInfo;
};