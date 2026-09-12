import 'dart:convert';
import 'package:http/http.dart' as http;
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

  Map<String, dynamic> _handleResponse(http.Response response) {
    final data = jsonDecode(response.body) as Map<String, dynamic>;
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return data;
    }
    throw AppException(data['message'] ?? 'An error occurred', response.statusCode);
  }
}
