# Invoice Email API - PHP Implementation Guide

This document provides the PHP code and setup instructions for the invoice email API endpoint used by Argo Books to send invoices via email.

## Overview

The Argo Books application sends invoice emails through an HTTP API endpoint. The API receives JSON payloads containing invoice HTML and recipient information, then uses your email server to send the emails.

## API Specification

### Endpoint
```
POST /api/invoice/send-email.php
```

### Request Headers
- `Content-Type: application/json`
- `X-Api-Key: your-api-key` (required for authentication)

### Request Body (JSON)
```json
{
  "to": "customer@example.com",
  "toName": "John Doe",
  "from": "billing@yourcompany.com",
  "fromName": "Your Company",
  "replyTo": "support@yourcompany.com",
  "subject": "Invoice INV-001 from Your Company",
  "htmlBody": "<html>...</html>",
  "plainTextBody": "Invoice #INV-001...",
  "invoiceId": "INV-001",
  "attachments": [
    {
      "filename": "invoice.pdf",
      "contentBase64": "base64-encoded-content",
      "mimeType": "application/pdf"
    }
  ]
}
```

### Response (JSON)
```json
{
  "success": true,
  "message": "Email sent successfully",
  "messageId": "unique-message-id"
}
```

### Error Response (JSON)
```json
{
  "success": false,
  "message": "Error description here",
  "messageId": null
}
```

## PHP Implementation

### 1. Main API File (`api/invoice/send-email.php`)

```php
<?php
/**
 * Invoice Email API Endpoint
 *
 * This endpoint handles sending invoice emails from the Argo Books application.
 *
 * @author Argo Books
 * @version 1.0.0
 */

// Enable error reporting for development (disable in production)
// error_reporting(E_ALL);
// ini_set('display_errors', 1);

// Set headers
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, X-Api-Key');

// Handle preflight OPTIONS request
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

// Only allow POST requests
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode([
        'success' => false,
        'message' => 'Method not allowed. Only POST requests are accepted.',
        'messageId' => null
    ]);
    exit;
}

// Load configuration
require_once __DIR__ . '/config.php';

// Verify API key
$apiKey = $_SERVER['HTTP_X_API_KEY'] ?? '';
if (empty($apiKey) || $apiKey !== INVOICE_API_KEY) {
    http_response_code(401);
    echo json_encode([
        'success' => false,
        'message' => 'Invalid or missing API key.',
        'messageId' => null
    ]);
    exit;
}

// Get JSON input
$input = file_get_contents('php://input');
$data = json_decode($input, true);

if (json_last_error() !== JSON_ERROR_NONE) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => 'Invalid JSON input: ' . json_last_error_msg(),
        'messageId' => null
    ]);
    exit;
}

// Validate required fields
$requiredFields = ['to', 'from', 'subject', 'htmlBody'];
$missingFields = [];
foreach ($requiredFields as $field) {
    if (empty($data[$field])) {
        $missingFields[] = $field;
    }
}

if (!empty($missingFields)) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => 'Missing required fields: ' . implode(', ', $missingFields),
        'messageId' => null
    ]);
    exit;
}

// Validate email addresses
if (!filter_var($data['to'], FILTER_VALIDATE_EMAIL)) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => 'Invalid recipient email address.',
        'messageId' => null
    ]);
    exit;
}

if (!filter_var($data['from'], FILTER_VALIDATE_EMAIL)) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'message' => 'Invalid sender email address.',
        'messageId' => null
    ]);
    exit;
}

// Include the email sender
require_once __DIR__ . '/InvoiceEmailSender.php';

try {
    $sender = new InvoiceEmailSender();
    $result = $sender->send($data);

    if ($result['success']) {
        http_response_code(200);
    } else {
        http_response_code(500);
    }

    echo json_encode($result);
} catch (Exception $e) {
    http_response_code(500);
    echo json_encode([
        'success' => false,
        'message' => 'Server error: ' . $e->getMessage(),
        'messageId' => null
    ]);
}
```

### 2. Configuration File (`api/invoice/config.php`)

```php
<?php
/**
 * Invoice Email API Configuration
 *
 * IMPORTANT: Keep this file secure and never commit with real credentials!
 */

// API Authentication
define('INVOICE_API_KEY', 'your-secure-api-key-here');

// Email Settings
define('SMTP_HOST', 'smtp.yourserver.com');
define('SMTP_PORT', 587);
define('SMTP_SECURE', 'tls'); // 'tls', 'ssl', or '' for none
define('SMTP_AUTH', true);
define('SMTP_USERNAME', 'your-smtp-username');
define('SMTP_PASSWORD', 'your-smtp-password');

// Default sender (can be overridden in request)
define('DEFAULT_FROM_EMAIL', 'noreply@yourcompany.com');
define('DEFAULT_FROM_NAME', 'Your Company');

// Rate limiting (optional)
define('RATE_LIMIT_ENABLED', true);
define('RATE_LIMIT_MAX_REQUESTS', 100); // Max requests per time window
define('RATE_LIMIT_TIME_WINDOW', 3600); // Time window in seconds (1 hour)

// Logging
define('LOG_ENABLED', true);
define('LOG_FILE', __DIR__ . '/logs/email.log');

// Security
define('MAX_ATTACHMENT_SIZE', 10 * 1024 * 1024); // 10MB max per attachment
define('ALLOWED_ATTACHMENT_TYPES', ['application/pdf', 'image/png', 'image/jpeg']);
```

### 3. Email Sender Class (`api/invoice/InvoiceEmailSender.php`)

```php
<?php
/**
 * Invoice Email Sender
 *
 * Handles the actual sending of invoice emails using PHPMailer.
 */

use PHPMailer\PHPMailer\PHPMailer;
use PHPMailer\PHPMailer\SMTP;
use PHPMailer\PHPMailer\Exception;

// Include PHPMailer (install via Composer: composer require phpmailer/phpmailer)
require_once __DIR__ . '/../../vendor/autoload.php';

class InvoiceEmailSender
{
    private $mailer;
    private $rateLimiter;

    public function __construct()
    {
        $this->mailer = new PHPMailer(true);
        $this->setupMailer();

        if (defined('RATE_LIMIT_ENABLED') && RATE_LIMIT_ENABLED) {
            $this->rateLimiter = new RateLimiter();
        }
    }

    private function setupMailer()
    {
        // Server settings
        $this->mailer->isSMTP();
        $this->mailer->Host = SMTP_HOST;
        $this->mailer->Port = SMTP_PORT;

        if (SMTP_AUTH) {
            $this->mailer->SMTPAuth = true;
            $this->mailer->Username = SMTP_USERNAME;
            $this->mailer->Password = SMTP_PASSWORD;
        }

        if (!empty(SMTP_SECURE)) {
            $this->mailer->SMTPSecure = SMTP_SECURE;
        }

        // Content settings
        $this->mailer->isHTML(true);
        $this->mailer->CharSet = 'UTF-8';
    }

    /**
     * Send an invoice email
     *
     * @param array $data Email data from API request
     * @return array Result with success, message, and messageId
     */
    public function send(array $data): array
    {
        // Check rate limit
        if ($this->rateLimiter && !$this->rateLimiter->check($data['from'])) {
            return [
                'success' => false,
                'message' => 'Rate limit exceeded. Please try again later.',
                'messageId' => null
            ];
        }

        try {
            // Clear any previous recipients
            $this->mailer->clearAddresses();
            $this->mailer->clearReplyTos();
            $this->mailer->clearAttachments();

            // Set sender
            $fromEmail = $data['from'] ?? DEFAULT_FROM_EMAIL;
            $fromName = $data['fromName'] ?? DEFAULT_FROM_NAME;
            $this->mailer->setFrom($fromEmail, $fromName);

            // Set recipient
            $toName = $data['toName'] ?? '';
            $this->mailer->addAddress($data['to'], $toName);

            // Set reply-to if provided
            if (!empty($data['replyTo'])) {
                $this->mailer->addReplyTo($data['replyTo']);
            }

            // Set subject
            $this->mailer->Subject = $data['subject'];

            // Set body
            $this->mailer->Body = $data['htmlBody'];

            // Set plain text alternative if provided
            if (!empty($data['plainTextBody'])) {
                $this->mailer->AltBody = $data['plainTextBody'];
            } else {
                // Generate plain text from HTML as fallback
                $this->mailer->AltBody = strip_tags(
                    str_replace(['<br>', '<br/>', '<br />', '</p>'], "\n", $data['htmlBody'])
                );
            }

            // Handle attachments
            if (!empty($data['attachments']) && is_array($data['attachments'])) {
                foreach ($data['attachments'] as $attachment) {
                    $this->addAttachment($attachment);
                }
            }

            // Generate a unique message ID
            $messageId = $this->generateMessageId($data['invoiceId'] ?? 'invoice');
            $this->mailer->MessageID = $messageId;

            // Send the email
            $this->mailer->send();

            // Log success
            $this->log('Email sent successfully', [
                'to' => $data['to'],
                'subject' => $data['subject'],
                'messageId' => $messageId,
                'invoiceId' => $data['invoiceId'] ?? 'N/A'
            ]);

            return [
                'success' => true,
                'message' => 'Email sent successfully.',
                'messageId' => $messageId
            ];

        } catch (Exception $e) {
            // Log error
            $this->log('Email sending failed', [
                'to' => $data['to'] ?? 'unknown',
                'error' => $e->getMessage()
            ], 'ERROR');

            return [
                'success' => false,
                'message' => 'Failed to send email: ' . $e->getMessage(),
                'messageId' => null
            ];
        }
    }

    /**
     * Add an attachment to the email
     *
     * @param array $attachment Attachment data
     */
    private function addAttachment(array $attachment): void
    {
        if (empty($attachment['contentBase64']) || empty($attachment['filename'])) {
            return;
        }

        // Validate MIME type
        $mimeType = $attachment['mimeType'] ?? 'application/octet-stream';
        if (!in_array($mimeType, ALLOWED_ATTACHMENT_TYPES)) {
            throw new Exception("Attachment type not allowed: {$mimeType}");
        }

        // Decode base64 content
        $content = base64_decode($attachment['contentBase64']);
        if ($content === false) {
            throw new Exception("Invalid base64 content for attachment: {$attachment['filename']}");
        }

        // Check size
        if (strlen($content) > MAX_ATTACHMENT_SIZE) {
            throw new Exception("Attachment too large: {$attachment['filename']}");
        }

        // Add as string attachment
        $this->mailer->addStringAttachment(
            $content,
            $attachment['filename'],
            PHPMailer::ENCODING_BASE64,
            $mimeType
        );
    }

    /**
     * Generate a unique message ID
     *
     * @param string $invoiceId Invoice ID for reference
     * @return string Message ID
     */
    private function generateMessageId(string $invoiceId): string
    {
        $domain = parse_url('http://' . ($_SERVER['HTTP_HOST'] ?? 'localhost'), PHP_URL_HOST);
        $timestamp = time();
        $random = bin2hex(random_bytes(8));
        return "<{$invoiceId}.{$timestamp}.{$random}@{$domain}>";
    }

    /**
     * Log email activity
     *
     * @param string $message Log message
     * @param array $context Additional context
     * @param string $level Log level
     */
    private function log(string $message, array $context = [], string $level = 'INFO'): void
    {
        if (!defined('LOG_ENABLED') || !LOG_ENABLED) {
            return;
        }

        $logDir = dirname(LOG_FILE);
        if (!is_dir($logDir)) {
            mkdir($logDir, 0755, true);
        }

        $timestamp = date('Y-m-d H:i:s');
        $contextStr = !empty($context) ? json_encode($context) : '';
        $logLine = "[{$timestamp}] [{$level}] {$message} {$contextStr}\n";

        file_put_contents(LOG_FILE, $logLine, FILE_APPEND | LOCK_EX);
    }
}

/**
 * Simple rate limiter using file-based storage
 */
class RateLimiter
{
    private $storageFile;

    public function __construct()
    {
        $this->storageFile = __DIR__ . '/logs/rate_limits.json';
    }

    /**
     * Check if request is within rate limits
     *
     * @param string $identifier Unique identifier (e.g., email or IP)
     * @return bool True if request is allowed
     */
    public function check(string $identifier): bool
    {
        $data = $this->loadData();
        $now = time();
        $window = RATE_LIMIT_TIME_WINDOW;
        $maxRequests = RATE_LIMIT_MAX_REQUESTS;

        // Clean up old entries
        $data = array_filter($data, function($entry) use ($now, $window) {
            return ($now - $entry['timestamp']) < $window;
        });

        // Count requests for this identifier
        $count = 0;
        foreach ($data as $entry) {
            if ($entry['identifier'] === $identifier) {
                $count++;
            }
        }

        if ($count >= $maxRequests) {
            return false;
        }

        // Add this request
        $data[] = [
            'identifier' => $identifier,
            'timestamp' => $now
        ];

        $this->saveData($data);
        return true;
    }

    private function loadData(): array
    {
        if (!file_exists($this->storageFile)) {
            return [];
        }

        $content = file_get_contents($this->storageFile);
        return json_decode($content, true) ?? [];
    }

    private function saveData(array $data): void
    {
        $dir = dirname($this->storageFile);
        if (!is_dir($dir)) {
            mkdir($dir, 0755, true);
        }

        file_put_contents($this->storageFile, json_encode($data), LOCK_EX);
    }
}
```

### 4. Alternative: Using Native PHP mail() Function

If you don't have PHPMailer or prefer a simpler solution, here's a version using PHP's built-in mail() function:

```php
<?php
/**
 * Simple Invoice Email Sender (no dependencies)
 */

class SimpleInvoiceEmailSender
{
    /**
     * Send an invoice email using PHP's mail() function
     *
     * @param array $data Email data
     * @return array Result
     */
    public function send(array $data): array
    {
        try {
            // Build headers
            $headers = [];
            $headers[] = "MIME-Version: 1.0";
            $headers[] = "Content-Type: text/html; charset=UTF-8";

            $fromName = $data['fromName'] ?? 'Invoice System';
            $fromEmail = $data['from'];
            $headers[] = "From: {$fromName} <{$fromEmail}>";

            if (!empty($data['replyTo'])) {
                $headers[] = "Reply-To: {$data['replyTo']}";
            }

            // Generate message ID
            $messageId = $this->generateMessageId($data['invoiceId'] ?? 'invoice');
            $headers[] = "Message-ID: {$messageId}";
            $headers[] = "X-Mailer: ArgoBooks/1.0";

            // Prepare recipient
            $to = $data['to'];
            if (!empty($data['toName'])) {
                $to = "{$data['toName']} <{$data['to']}>";
            }

            // Send
            $success = mail(
                $to,
                $data['subject'],
                $data['htmlBody'],
                implode("\r\n", $headers)
            );

            if ($success) {
                return [
                    'success' => true,
                    'message' => 'Email sent successfully.',
                    'messageId' => $messageId
                ];
            } else {
                return [
                    'success' => false,
                    'message' => 'Failed to send email.',
                    'messageId' => null
                ];
            }

        } catch (Exception $e) {
            return [
                'success' => false,
                'message' => 'Error: ' . $e->getMessage(),
                'messageId' => null
            ];
        }
    }

    private function generateMessageId(string $invoiceId): string
    {
        $domain = $_SERVER['HTTP_HOST'] ?? 'localhost';
        $timestamp = time();
        $random = bin2hex(random_bytes(8));
        return "<{$invoiceId}.{$timestamp}.{$random}@{$domain}>";
    }
}
```

## Directory Structure

```
api/
└── invoice/
    ├── send-email.php      # Main API endpoint
    ├── config.php          # Configuration (DO NOT commit with credentials)
    ├── InvoiceEmailSender.php  # Email sending logic
    └── logs/               # Log directory (create with 755 permissions)
        ├── email.log
        └── rate_limits.json
```

## Setup Instructions

### 1. Install PHPMailer (Recommended)

```bash
cd your-website-root
composer require phpmailer/phpmailer
```

### 2. Configure the API

1. Copy `config.php.example` to `config.php`
2. Edit `config.php` with your SMTP settings:
   - Set a secure `INVOICE_API_KEY`
   - Configure SMTP credentials
   - Adjust rate limits as needed

### 3. Create Logs Directory

```bash
mkdir -p api/invoice/logs
chmod 755 api/invoice/logs
```

### 4. Secure the API

Add this to your `.htaccess` to protect the config file:

```apache
<Files "config.php">
    Order Allow,Deny
    Deny from all
</Files>

<Files "*.log">
    Order Allow,Deny
    Deny from all
</Files>
```

### 5. Configure Argo Books

Argo Books loads API credentials from a `.env` file (same approach as telemetry and other services).

Add the following to your `.env` file in the Argo Books application directory:

```env
# Invoice Email API Configuration
INVOICE_EMAIL_API_ENDPOINT=https://yourwebsite.com/api/invoice/send-email.php
INVOICE_EMAIL_API_KEY=your-secure-api-key-here
```

**Important**: The API key must match the one you set in `config.php` on your website.

Company-specific settings (From Name, From Email, etc.) are configured within each company file through the Argo Books UI.

## Testing

### Test with cURL

```bash
curl -X POST https://yourwebsite.com/api/invoice/send-email.php \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-api-key" \
  -d '{
    "to": "test@example.com",
    "from": "billing@yourcompany.com",
    "fromName": "Your Company",
    "subject": "Test Invoice Email",
    "htmlBody": "<html><body><h1>Test Invoice</h1><p>This is a test.</p></body></html>"
  }'
```

### Expected Response

```json
{
  "success": true,
  "message": "Email sent successfully.",
  "messageId": "<invoice.1234567890.abc123@yourwebsite.com>"
}
```

## Troubleshooting

### Common Issues

1. **401 Unauthorized**
   - Check that the API key in your `.env` file (`INVOICE_EMAIL_API_KEY`) matches `config.php`
   - Ensure the X-Api-Key header is being sent

1. **"Email API is not configured" error in Argo Books**
   - Make sure your `.env` file contains both `INVOICE_EMAIL_API_ENDPOINT` and `INVOICE_EMAIL_API_KEY`
   - Verify the `.env` file is in the correct location (application directory or parent directories)

2. **500 Internal Server Error**
   - Check PHP error logs
   - Verify SMTP credentials
   - Ensure PHPMailer is installed

3. **Email not arriving**
   - Check spam folder
   - Verify SMTP settings
   - Check the logs/email.log file

4. **Rate limit exceeded**
   - Wait for the time window to reset
   - Increase RATE_LIMIT_MAX_REQUESTS in config.php

### Debug Mode

Enable detailed error reporting for debugging:

```php
// Add to the top of send-email.php
error_reporting(E_ALL);
ini_set('display_errors', 1);

// Also enable PHPMailer debug output
$this->mailer->SMTPDebug = SMTP::DEBUG_SERVER;
```

**Important**: Disable debug mode in production!

## Security Best Practices

1. **Always use HTTPS** for the API endpoint
2. **Never commit** `config.php` with real credentials
3. **Use strong API keys** (32+ characters, random)
4. **Enable rate limiting** to prevent abuse
5. **Validate all inputs** on the server side
6. **Keep PHPMailer updated** to the latest version
7. **Monitor logs** for suspicious activity
