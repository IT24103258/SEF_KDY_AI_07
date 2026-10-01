import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../core/routes/app_router.dart';

/// Registration screen — /register.
///
/// Mirrors the React Register page: same POST /api/auth/register contract,
/// same client-side validation rules, and the same auto-login-after-register
/// flow. Only Requester and Technician accounts can be self-registered
/// (backend seeds Manager/Administrator separately).
class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();

  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();

  String _roleName = 'Requester';
  bool _obscurePassword = true;
  bool _obscureConfirm = true;

  static final RegExp _emailRe = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');
  static final RegExp _phoneRe = RegExp(r'^\+?[0-9]{9,15}$');

  @override
  void dispose() {
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final authProvider = context.read<AuthProvider>();
    final success = await authProvider.register(
      firstName: _firstNameCtrl.text.trim(),
      lastName: _lastNameCtrl.text.trim(),
      email: _emailCtrl.text.trim(),
      phoneNumber: _phoneCtrl.text.trim(),
      password: _passwordCtrl.text,
      roleName: _roleName,
    );

    if (!mounted) return;
    if (success) {
      Navigator.pushReplacementNamed(
          context, AppRouter.homeRouteFor(authProvider.user?.role));
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Create Account'),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Icon(Icons.build_circle,
                    size: 56, color: Color(0xFF2563EB)),
                const SizedBox(height: 12),
                const Text(
                  'Join FixFlow',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
                ),
                const Text(
                  'Register as a customer to raise requests, or as a '
                  'technician to get assigned work.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Colors.grey),
                ),
                const SizedBox(height: 24),

                if (authProvider.error != null)
                  Container(
                    padding: const EdgeInsets.all(12),
                    margin: const EdgeInsets.only(bottom: 16),
                    decoration: BoxDecoration(
                      color: Colors.red.shade100,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      authProvider.error!,
                      style: TextStyle(color: Colors.red.shade900),
                    ),
                  ),

                DropdownButtonFormField<String>(
                  initialValue: _roleName,
                  decoration: const InputDecoration(
                    labelText: "I'm registering as",
                    border: OutlineInputBorder(),
                    prefixIcon: Icon(Icons.badge_outlined),
                  ),
                  items: const [
                    DropdownMenuItem(
                      value: 'Requester',
                      child: Text('Customer — I want to submit requests'),
                    ),
                    DropdownMenuItem(
                      value: 'Technician',
                      child: Text('Technician — I carry out work orders'),
                    ),
                  ],
                  onChanged: authProvider.isLoading
                      ? null
                      : (v) => setState(() => _roleName = v ?? 'Requester'),
                ),
                const SizedBox(height: 16),

                Row(children: [
                  Expanded(
                    child: TextFormField(
                      controller: _firstNameCtrl,
                      enabled: !authProvider.isLoading,
                      decoration: const InputDecoration(
                        labelText: 'First Name *',
                        border: OutlineInputBorder(),
                        prefixIcon: Icon(Icons.person_outline),
                      ),
                      validator: (v) =>
                          (v == null || v.trim().isEmpty)
                              ? 'First name is required'
                              : null,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextFormField(
                      controller: _lastNameCtrl,
                      enabled: !authProvider.isLoading,
                      decoration: const InputDecoration(
                        labelText: 'Last Name *',
                        border: OutlineInputBorder(),
                        prefixIcon: Icon(Icons.person_outline),
                      ),
                      validator: (v) =>
                          (v == null || v.trim().isEmpty)
                              ? 'Last name is required'
                              : null,
                    ),
                  ),
                ]),
                const SizedBox(height: 16),

                TextFormField(
                  controller: _emailCtrl,
                  enabled: !authProvider.isLoading,
                  keyboardType: TextInputType.emailAddress,
                  decoration: const InputDecoration(
                    labelText: 'Email Address *',
                    hintText: 'you@example.com',
                    border: OutlineInputBorder(),
                    prefixIcon: Icon(Icons.email),
                  ),
                  validator: (v) {
                    if (v == null || v.trim().isEmpty) {
                      return 'Email is required';
                    }
                    if (!_emailRe.hasMatch(v.trim())) {
                      return 'Enter a valid email address';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                TextFormField(
                  controller: _phoneCtrl,
                  enabled: !authProvider.isLoading,
                  keyboardType: TextInputType.phone,
                  decoration: const InputDecoration(
                    labelText: 'Phone Number *',
                    hintText: '+94 77 123 4567',
                    border: OutlineInputBorder(),
                    prefixIcon: Icon(Icons.phone),
                  ),
                  validator: (v) {
                    final cleaned =
                        (v ?? '').replaceAll(RegExp(r'[\s\-()]'), '');
                    if (cleaned.isEmpty) return 'Phone number is required';
                    if (!_phoneRe.hasMatch(cleaned)) {
                      return 'Enter a valid phone number';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                TextFormField(
                  controller: _passwordCtrl,
                  enabled: !authProvider.isLoading,
                  obscureText: _obscurePassword,
                  decoration: InputDecoration(
                    labelText: 'Password *',
                    hintText: 'At least 8 characters',
                    border: const OutlineInputBorder(),
                    prefixIcon: const Icon(Icons.lock),
                    suffixIcon: IconButton(
                      icon: Icon(_obscurePassword
                          ? Icons.visibility_off
                          : Icons.visibility),
                      onPressed: () => setState(
                          () => _obscurePassword = !_obscurePassword),
                    ),
                  ),
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Password is required';
                    if (v.length < 8) return 'Use at least 8 characters';
                    if (!RegExp(r'[A-Za-z]').hasMatch(v) ||
                        !RegExp(r'[0-9]').hasMatch(v)) {
                      return 'Include at least one letter and one number';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                TextFormField(
                  controller: _confirmCtrl,
                  enabled: !authProvider.isLoading,
                  obscureText: _obscureConfirm,
                  decoration: InputDecoration(
                    labelText: 'Confirm Password *',
                    border: const OutlineInputBorder(),
                    prefixIcon: const Icon(Icons.lock_outline),
                    suffixIcon: IconButton(
                      icon: Icon(_obscureConfirm
                          ? Icons.visibility_off
                          : Icons.visibility),
                      onPressed: () =>
                          setState(() => _obscureConfirm = !_obscureConfirm),
                    ),
                  ),
                  validator: (v) =>
                      v != _passwordCtrl.text ? 'Passwords do not match' : null,
                ),
                const SizedBox(height: 24),

                SizedBox(
                  height: 48,
                  child: ElevatedButton(
                    onPressed: authProvider.isLoading ? null : _submit,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF2563EB),
                      foregroundColor: Colors.white,
                    ),
                    child: authProvider.isLoading
                        ? const SizedBox(
                            width: 22,
                            height: 22,
                            child: CircularProgressIndicator(
                                strokeWidth: 2, color: Colors.white),
                          )
                        : const Text('Create Account',
                            style: TextStyle(fontSize: 16)),
                  ),
                ),
                const SizedBox(height: 16),

                TextButton(
                  onPressed: authProvider.isLoading
                      ? null
                      : () =>
                          Navigator.pushReplacementNamed(context, AppRouter.login),
                  child: const Text(
                      'Already have an account? Sign in'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
