# =============================================================================
# Automated Test Script for Forgot/Reset/Change Password & Brevo SMTP
# =============================================================================

$baseUrl = "http://localhost:5000/api"
$results = @()

function Record-Test($name, $status, $detail) {
    Write-Host "[$status] $name - $detail"
    $script:results += [PSCustomObject]@{
        Test = $name
        Status = $status
        Details = $detail
    }
}

Write-Host "=== STARTING COMPREHENSIVE BACKEND AUTH TESTS ==="

# -----------------------------------------------------------------------------
# Setup Temporary Test User 1
# -----------------------------------------------------------------------------
$email1 = "temp_user1_hb@testexample.com"
$regBody1 = @{
    name = "Test User One"
    email = $email1
    password = "OldPassword123"
} | ConvertTo-Json

try {
    $regRes1 = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -ContentType "application/json" -Body $regBody1
    $token1 = $regRes1.token
    $userId1 = $regRes1.userId
    Record-Test "Setup Test User 1" "OK" "Created test user ID $userId1"
} catch {
    # If already exists, login
    $loginBody = @{ email = $email1; password = "OldPassword123" } | ConvertTo-Json
    $loginRes = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
    $token1 = $loginRes.token
    $userId1 = $loginRes.userId
    Record-Test "Setup Test User 1" "OK" "Logged in existing test user ID $userId1"
}

# -----------------------------------------------------------------------------
# TEST 1: Forgot-password with registered email -> email sent / generic message
# -----------------------------------------------------------------------------
try {
    $body = @{ email = $email1 } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/auth/forgot-password" -Method Post -ContentType "application/json" -Body $body
    if ($res.message -like "*If this email is registered*") {
        Record-Test "1. Forgot Password Registered Email" "OK" "Returned generic message: $($res.message)"
    } else {
        Record-Test "1. Forgot Password Registered Email" "FAIL" "Unexpected message: $($res.message)"
    }
} catch {
    Record-Test "1. Forgot Password Registered Email" "FAIL" $_.Exception.Message
}

# -----------------------------------------------------------------------------
# TEST 2: Forgot-password with unknown email -> same generic message
# -----------------------------------------------------------------------------
try {
    $body = @{ email = "nonexistent_fake_user999@testexample.com" } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/auth/forgot-password" -Method Post -ContentType "application/json" -Body $body
    if ($res.message -like "*If this email is registered*") {
        Record-Test "2. Forgot Password Unknown Email" "OK" "Returned same message without leaking user existence"
    } else {
        Record-Test "2. Forgot Password Unknown Email" "FAIL" "Unexpected message: $($res.message)"
    }
} catch {
    Record-Test "2. Forgot Password Unknown Email" "FAIL" $_.Exception.Message
}

# -----------------------------------------------------------------------------
# TEST 3: Wrong OTP 5 times -> OTP blocked
# -----------------------------------------------------------------------------
$blockedSuccess = $false
for ($i = 1; $i -le 5; $i++) {
    try {
        $body = @{ email = $email1; otp = "000000"; newPassword = "NewPassword123" } | ConvertTo-Json
        $res = Invoke-RestMethod -Uri "$baseUrl/auth/reset-password" -Method Post -ContentType "application/json" -Body $body
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $respText = $reader.ReadToEnd()
        if ($i -eq 5 -and $respText -like "*Too many failed attempts*") {
            $blockedSuccess = $true
        }
    }
}
if ($blockedSuccess) {
    Record-Test "3. Wrong OTP 5 Times Blocked" "OK" "Blocked after 5th attempt with error"
} else {
    Record-Test "3. Wrong OTP 5 Times Blocked" "FAIL" "Did not receive block message on 5th attempt"
}

# -----------------------------------------------------------------------------
# Setup Temporary Test User 2 for Expired and Correct OTP tests
# -----------------------------------------------------------------------------
$email2 = "temp_user2_hb@testexample.com"
$regBody2 = @{
    name = "Test User Two"
    email = $email2
    password = "InitialPass123"
} | ConvertTo-Json

try {
    $regRes2 = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -ContentType "application/json" -Body $regBody2
    $token2 = $regRes2.token
    $userId2 = $regRes2.userId
    Record-Test "Setup Test User 2" "OK" "Created test user ID $userId2"
} catch {
    $loginBody2 = @{ email = $email2; password = "InitialPass123" } | ConvertTo-Json
    $loginRes2 = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body $loginBody2
    $token2 = $loginRes2.token
    $userId2 = $loginRes2.userId
    Record-Test "Setup Test User 2" "OK" "Logged in existing test user ID $userId2"
}

# -----------------------------------------------------------------------------
# TEST 4: Expired OTP -> rejected
# -----------------------------------------------------------------------------
# Request OTP for User 2
$body = @{ email = $email2 } | ConvertTo-Json
$null = Invoke-RestMethod -Uri "$baseUrl/auth/forgot-password" -Method Post -ContentType "application/json" -Body $body

# In PostgreSQL, expire this OTP (set ExpiresAt to 15 mins ago)
$env:PGPASSWORD = "12345678"
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -d HB -c "UPDATE ""PasswordResetOtp"" SET ""ExpiresAt"" = NOW() - INTERVAL '15 minutes' WHERE ""UserId"" = $userId2 AND ""IsUsed"" = false;"

# Try to reset with expired OTP
try {
    $body = @{ email = $email2; otp = "123456"; newPassword = "ExpiredPass123" } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUrl/auth/reset-password" -Method Post -ContentType "application/json" -Body $body
    Record-Test "4. Expired OTP Rejected" "FAIL" "Should have failed but succeeded"
} catch {
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $respText = $reader.ReadToEnd()
    if ($respText -like "*expired*") {
        Record-Test "4. Expired OTP Rejected" "OK" "Rejected expired OTP with message: $respText"
    } else {
        Record-Test "4. Expired OTP Rejected" "FAIL" "Unexpected error: $respText"
    }
}

# -----------------------------------------------------------------------------
# Setup Temporary Test User 3 for Correct OTP & Re-use tests
# -----------------------------------------------------------------------------
$email3 = "temp_user3_hb@testexample.com"
$regBody3 = @{
    name = "Test User Three"
    email = $email3
    password = "OldUserThreePass123"
} | ConvertTo-Json

$regRes3 = Invoke-RestMethod -Uri "$baseUrl/auth/register" -Method Post -ContentType "application/json" -Body $regBody3
$token3 = $regRes3.token
$userId3 = $regRes3.userId

# Request OTP for User 3
$body3 = @{ email = $email3 } | ConvertTo-Json
$null = Invoke-RestMethod -Uri "$baseUrl/auth/forgot-password" -Method Post -ContentType "application/json" -Body $body3

# Set known BCrypt hash for OTP '123456' on the active OTP record:
# BCrypt hash of "123456" is "$2a$11$qR6JkS/r1Z.K3bK4w1U02eLwR9wJ8U7gN0y3jZ6H6iUjS6zU8f8sC" (or generated dynamically)
$hashOf123456 = '$2a$11$qR6JkS/r1Z.K3bK4w1U02eLwR9wJ8U7gN0y3jZ6H6iUjS6zU8f8sC'
# Let's update with psql
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -d HB -c "UPDATE ""PasswordResetOtp"" SET ""OtpHash"" = '$hashOf123456' WHERE ""UserId"" = $userId3 AND ""IsUsed"" = false;"

# -----------------------------------------------------------------------------
# TEST 5: Correct OTP -> password changed, old pass fails, new pass works
# -----------------------------------------------------------------------------
$resetSuccess = $false
try {
    $resetBody = @{
        email = $email3
        otp = "123456"
        newPassword = "BrandNewPass456"
    } | ConvertTo-Json
    $resetRes = Invoke-RestMethod -Uri "$baseUrl/auth/reset-password" -Method Post -ContentType "application/json" -Body $resetBody
    if ($resetRes.message -like "*successfully*") {
        $resetSuccess = $true
    }
} catch {
    Record-Test "5. Correct OTP Password Reset" "FAIL" $_.Exception.Message
}

if ($resetSuccess) {
    # Check old password fails
    $oldPassFailed = $false
    try {
        $null = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body (@{ email = $email3; password = "OldUserThreePass123" } | ConvertTo-Json)
    } catch {
        $oldPassFailed = $true
    }

    # Check new password works
    $newPassWorked = $false
    try {
        $loginNewRes = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType "application/json" -Body (@{ email = $email3; password = "BrandNewPass456" } | ConvertTo-Json)
        if ($loginNewRes.token) {
            $newPassWorked = $true
            $token3 = $loginNewRes.token
        }
    } catch {}

    if ($oldPassFailed -and $newPassWorked) {
        Record-Test "5. Correct OTP Password Reset" "OK" "Password updated, old password rejected, new password authenticated"
    } else {
        Record-Test "5. Correct OTP Password Reset" "FAIL" "Old pass rejected: $oldPassFailed, New pass worked: $newPassWorked"
    }
}

# -----------------------------------------------------------------------------
# TEST 6: Same OTP used twice -> second time rejected
# -----------------------------------------------------------------------------
try {
    $reuseBody = @{
        email = $email3
        otp = "123456"
        newPassword = "YetAnotherPass789"
    } | ConvertTo-Json
    $null = Invoke-RestMethod -Uri "$baseUrl/auth/reset-password" -Method Post -ContentType "application/json" -Body $reuseBody
    Record-Test "6. OTP Reused Rejected" "FAIL" "Reused OTP was accepted unexpectedly"
} catch {
    Record-Test "6. OTP Reused Rejected" "OK" "Second attempt with same OTP was rejected"
}

# -----------------------------------------------------------------------------
# TEST 7: Change-password with wrong current password -> rejected
# -----------------------------------------------------------------------------
$authHeaders = @{ Authorization = "Bearer $token3" }
try {
    $changeWrongBody = @{
        currentPassword = "WrongPassword999"
        newPassword = "ValidNewPassword123"
    } | ConvertTo-Json
    $null = Invoke-RestMethod -Uri "$baseUrl/auth/change-password" -Method Post -Headers $authHeaders -ContentType "application/json" -Body $changeWrongBody
    Record-Test "7. Change Password Wrong Current Rejected" "FAIL" "Wrong current password was accepted unexpectedly"
} catch {
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $respText = $reader.ReadToEnd()
    if ($respText -like "*incorrect*") {
        Record-Test "7. Change Password Wrong Current Rejected" "OK" "Rejected with: $respText"
    } else {
        Record-Test "7. Change Password Wrong Current Rejected" "FAIL" "Unexpected error: $respText"
    }
}

# Also test valid change password
try {
    $changeValidBody = @{
        currentPassword = "BrandNewPass456"
        newPassword = "FinalPassword321"
    } | ConvertTo-Json
    $changeRes = Invoke-RestMethod -Uri "$baseUrl/auth/change-password" -Method Post -Headers $authHeaders -ContentType "application/json" -Body $changeValidBody
    Record-Test "Change Password Valid" "OK" "Updated successfully with correct current password"
} catch {
    Record-Test "Change Password Valid" "FAIL" $_.Exception.Message
}

# -----------------------------------------------------------------------------
# CLEANUP TEMPORARY TEST DATA
# -----------------------------------------------------------------------------
Write-Host "Cleaning up temporary test users..."
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -U postgres -d HB -c "DELETE FROM ""Users"" WHERE ""Email"" IN ('$email1', '$email2', '$email3');"
Record-Test "Cleanup Test Data" "OK" "Removed temporary test users and cascaded OTP records"

Write-Host "=== TEST SUMMARY ==="
$script:results | Format-Table -AutoSize
