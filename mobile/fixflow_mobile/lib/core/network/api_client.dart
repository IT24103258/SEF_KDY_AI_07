import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import '../constants/api_constants.dart';
import '../errors/app_exception.dart';
import '../storage/secure_storage_service.dart';

class ApiClient {
  final http.Client _client;
  final SecureStorageService _storage;

  ApiClient({http.Client? client, SecureStorageService? storage})
      : _client = client ?? http.Client(),
        _storage = storage ?? SecureStorageService();

  Future<Map<String, dynamic>> get(String endpoint) async {
    final token = await _storage.getToken();
    final response = await _client.get(
      Uri.parse('${ApiConstants.baseUrl}$endpoint'),
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );
    return _handleResponse(response);
  }

  Future<Map<String, dynamic>> post(String endpoint, Map<String, dynamic> body) async {
    final token = await _storage.getToken();
    final response = await _client.post(
      Uri.parse('${ApiConstants.baseUrl}$endpoint'),
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
      body: jsonEncode(body),
    );
    return _handleResponse(response);
  }

  Future<Map<String, dynamic>> put(String endpoint, Map<String, dynamic> body) async {
    final token = await _storage.getToken();
    final response = await _client.put(
      Uri.parse('${ApiConstants.baseUrl}$endpoint'),
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
      body: jsonEncode(body),
    );
    return _handleResponse(response);
  }

  
  Future<Map<String, dynamic>> delete(String endpoint) async {
    final token = await _storage.getToken();
    final response = await _client.delete(
      Uri.parse('${ApiConstants.baseUrl}$endpoint'),
      headers: {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      },
    );
    return _handleResponse(response);
  }

  Future<Map<String, dynamic>> uploadFile(
      String endpoint, List<int> bytes, String fileName) async {
    final token = await _storage.getToken();
    final uri = Uri.parse('${ApiConstants.baseUrl}$endpoint');
    final request = http.MultipartRequest('POST', uri);

    if (token != null) {
      request.headers['Authorization'] = 'Bearer $token';
    }

    // The backend validates the part's content type (image/jpeg|png|jpg only).
    // Without this the part defaults to application/octet-stream and the
    // upload is rejected with "Only JPG and PNG images are allowed."
    request.files.add(
      http.MultipartFile.fromBytes(
        'file',
        bytes,
        filename: fileName,
        contentType: MediaType.parse(_contentTypeFor(fileName)),
      ),
    );

    final streamedResponse = await _client.send(request);
    final response = await http.Response.fromStream(streamedResponse);
    return _handleResponse(response);
  }

  String _contentTypeFor(String fileName) {
    final ext = fileName.toLowerCase().split('.').last;
    if (ext == 'png') return 'image/png';
    return 'image/jpeg';
  }

  Map<String, dynamic> _handleResponse(http.Response response) {
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return data;
    }
    throw AppException(_errorMessage(data), response.statusCode);
  }

  /// Failures arrive in two shapes: the FixFlow ApiResponse envelope
  /// (`{ message, errors: [...] }`) and the ASP.NET ProblemDetails emitted by
  /// FluentValidation auto-validation (`{ title, errors: { field: [...] } }`).
  /// ProblemDetails has no `message`, so reading only that key discarded the
  /// server's actual reason and left the UI showing "An error occurred".
  static String _errorMessage(Map<String, dynamic> data) {
    final message = data['message'];
    if (message is String && message.trim().isNotEmpty) return message;

    final errors = data['errors'];
    if (errors is List) {
      final texts =
          errors.whereType<String>().where((e) => e.trim().isNotEmpty).toList();
      if (texts.isNotEmpty) return texts.join(' • ');
    } else if (errors is Map) {
      final texts = <String>[];
      for (final value in errors.values) {
        if (value is List) {
          texts.addAll(value.whereType<String>());
        } else if (value is String && value.trim().isNotEmpty) {
          texts.add(value);
        }
      }
      if (texts.isNotEmpty) return texts.join(' • ');
    }

    final title = data['title'];
    if (title is String && title.trim().isNotEmpty) return title;
    return 'An error occurred';
  }
}
