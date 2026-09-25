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
  bool _isInitializing = true;
  String? _error;

  AuthProvider({ApiClient? apiClient, SecureStorageService? storage})
      : _apiClient = apiClient ?? ApiClient(),
        _storage = storage ?? SecureStorageService();

  UserModel? get user => _user;
  bool get isLoading => _isLoading;
  bool get isInitializing => _isInitializing;
  bool get isAuthenticated => _user != null;
  String? get error => _error;

  Future<void> tryAutoLogin() async {
    _isInitializing = true;
    notifyListeners();

    try {
      final token = await _storage.getToken();
      if (token != null && token.isNotEmpty) {
        final res = await _apiClient.get(ApiConstants.currentUser);
        if (res['success'] == true && res['data'] != null) {
          final user = UserModel.fromJson(res['data']);
          if (user.isTechnician) {
            _user = user;
          } else {
            await _storage.deleteToken();
            _user = null;
          }
        }
      }
    } catch (_) {
      _user = null;
    } finally {
      _isInitializing = false;
      notifyListeners();
    }
  }

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final res = await _apiClient.post(ApiConstants.login, {
        'email': email.trim(),
        'password': password,
      });

      if (res['success'] == true && res['data'] != null) {
        final user = UserModel.fromJson(res['data']['user']);
        
        if (!user.isTechnician) {
          _error = 'Access Denied: Only Technician accounts can access this mobile application.';
          _isLoading = false;
          notifyListeners();
          return false;
        }

        final token = res['data']['token'] as String;
        await _storage.saveToken(token);
        _user = user;
        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        _error = res['message'] ?? 'Invalid credentials.';
      }
    } catch (e) {
      _error = e.toString().replaceAll('Exception: ', '').replaceAll('AppException: ', '');
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<void> logout() async {
    await _storage.deleteToken();
    _user = null;
    _error = null;
    notifyListeners();
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
