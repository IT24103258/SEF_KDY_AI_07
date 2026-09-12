import 'package:flutter/material.dart';
import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../core/storage/secure_storage_service.dart';
import '../models/user_model.dart';

class AuthProvider with ChangeNotifier {
  final ApiClient _apiClient;
  final SecureStorageService _storage;

  UserModel? _user;
  bool _isLoading = false;
  String? _error;

  AuthProvider({ApiClient? apiClient, SecureStorageService? storage})
      : _apiClient = apiClient ?? ApiClient(),
        _storage = storage ?? SecureStorageService();

  UserModel? get user => _user;
  bool get isLoading => _isLoading;
  bool get isAuthenticated => _user != null;
  String? get error => _error;

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final res = await _apiClient.post(ApiConstants.login, {
        'email': email,
        'password': password,
      });

      if (res['success'] == true && res['data'] != null) {
        final token = res['data']['token'] as String;
        await _storage.saveToken(token);
        _user = UserModel.fromJson(res['data']['user']);
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      _error = e.toString();
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<void> logout() async {
    await _storage.deleteToken();
    _user = null;
    notifyListeners();
  }
}
