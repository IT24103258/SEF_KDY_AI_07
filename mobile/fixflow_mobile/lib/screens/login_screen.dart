import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../core/routes/app_router.dart';
import '../widgets/auth_page_scaffold.dart';

/// Login screen — /login.
///
/// POSTs to the existing /api/auth/login endpoint through AuthProvider, which
/// persists the returned JWT in secure storage and exposes the signed-in user.
/// Credentials are never pre-filled or stored on the client.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  static final RegExp _emailRe = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();

    final authProvider = context.read<AuthProvider>();
    final success = await authProvider.login(
      _emailController.text.trim(),
      _passwordController.text,
    );

    if (!mounted || !success) return;
    Navigator.pushReplacementNamed(
      context,
      AppRouter.homeRouteFor(authProvider.user?.role),
    );
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = context.watch<AuthProvider>();
    final isLoading = authProvider.isLoading;

    return AuthPageScaffold(
      title: 'FixFlow AI',
      subtitle: 'Field Technician & Requester Portal',
      children: [
        if (authProvider.error != null) ...[
          AuthErrorBanner(
            key: const Key('login_error_banner'),
            message: authProvider.error!,
          ),
          const SizedBox(height: 18),
        ],
        Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                key: const Key('login_email_field'),
                controller: _emailController,
                enabled: !isLoading,
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email],
                decoration: const InputDecoration(
                  labelText: 'Email Address',
                  hintText: 'you@example.com',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.alternate_email),
                ),
                validator: (v) {
                  final email = (v ?? '').trim();
                  if (email.isEmpty) return 'Email is required';
                  if (!_emailRe.hasMatch(email)) {
                    return 'Enter a valid email address';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                key: const Key('login_password_field'),
                controller: _passwordController,
                enabled: !isLoading,
                obscureText: _obscurePassword,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.password],
                onFieldSubmitted: isLoading ? null : (_) => _submit(),
                decoration: InputDecoration(
                  labelText: 'Password',
                  border: const OutlineInputBorder(),
                  prefixIcon: const Icon(Icons.lock_outline),
                  suffixIcon: IconButton(
                    key: const Key('login_password_toggle'),
                    icon: Icon(
                      _obscurePassword
                          ? Icons.visibility_off_outlined
                          : Icons.visibility_outlined,
                    ),
                    tooltip: _obscurePassword
                        ? 'Show password'
                        : 'Hide password',
                    onPressed: () => setState(
                        () => _obscurePassword = !_obscurePassword),
                  ),
                ),
                validator: (v) => (v == null || v.isEmpty)
                    ? 'Password is required'
                    : null,
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
        SizedBox(
          height: 50,
          child: ElevatedButton(
            key: const Key('login_submit_button'),
            onPressed: isLoading ? null : _submit,
            style: ElevatedButton.styleFrom(
              backgroundColor: AuthPageScaffold.brandBlue,
              foregroundColor: Colors.white,
              disabledBackgroundColor:
                  AuthPageScaffold.brandBlue.withValues(alpha: 0.55),
              disabledForegroundColor: Colors.white70,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
            ),
            child: isLoading
                ? const SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : const Text(
                    'Sign In',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                  ),
          ),
        ),
        const SizedBox(height: 6),
        TextButton(
          key: const Key('login_register_link'),
          onPressed: isLoading
              ? null
              : () => Navigator.pushNamed(context, AppRouter.register),
          child: const Text("Don't have an account? Register"),
        ),
      ],
    );
  }
}
