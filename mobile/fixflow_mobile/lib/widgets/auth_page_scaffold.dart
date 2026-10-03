import 'package:flutter/material.dart';
import '../core/routes/app_router.dart';

/// Shared chrome for the Login and Register screens.
///
/// Both screens must present identical FixFlow branding, background and card
/// treatment, so the gradient surface, brand header, form card and error
/// banner live here instead of being duplicated per screen.
class AuthPageScaffold extends StatelessWidget {
  final String title;
  final String subtitle;
  final bool showBackButton;
  final List<Widget> children;

  const AuthPageScaffold({
    super.key,
    required this.title,
    required this.subtitle,
    this.showBackButton = false,
    required this.children,
  });

  static const Color brandBlue = Color(0xFF2563EB);
  static const Color brandDeep = Color(0xFF1E3A8A);

  /// Keeps the form at a readable width on web / large screens while letting
  /// it fill the viewport on a phone.
  static const double _maxFormWidth = 440;

  /// Returns to the login screen without stacking a second copy of it when
  /// Login is already underneath Register on the navigator.
  static void backToLogin(BuildContext context) {
    final navigator = Navigator.of(context);
    if (navigator.canPop()) {
      navigator.pop();
    } else {
      navigator.pushReplacementNamed(AppRouter.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [brandBlue, brandDeep],
          ),
        ),
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 28),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: _maxFormWidth),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (showBackButton)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: Align(
                          alignment: Alignment.centerLeft,
                          child: _AuthBackButton(onPressed: () => backToLogin(context)),
                        ),
                      ),
                    _BrandHeader(title: title, subtitle: subtitle),
                    const SizedBox(height: 24),
                    Container(
                      padding: const EdgeInsets.all(22),
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.surface,
                        borderRadius: BorderRadius.circular(20),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withValues(alpha: 0.20),
                            blurRadius: 24,
                            offset: const Offset(0, 10),
                          ),
                        ],
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: children,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _BrandHeader extends StatelessWidget {
  final String title;
  final String subtitle;

  const _BrandHeader({required this.title, required this.subtitle});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          height: 72,
          width: 72,
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.16),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: Colors.white.withValues(alpha: 0.35)),
          ),
          child: const Icon(
            Icons.build_circle_outlined,
            size: 42,
            color: Colors.white,
          ),
        ),
        const SizedBox(height: 16),
        Text(
          title,
          textAlign: TextAlign.center,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 26,
            fontWeight: FontWeight.w700,
            letterSpacing: -0.4,
          ),
        ),
        const SizedBox(height: 6),
        Text(
          subtitle,
          textAlign: TextAlign.center,
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.85),
            fontSize: 13.5,
            height: 1.45,
          ),
        ),
      ],
    );
  }
}

class _AuthBackButton extends StatelessWidget {
  final VoidCallback onPressed;

  const _AuthBackButton({required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white.withValues(alpha: 0.18),
      shape: const CircleBorder(),
      clipBehavior: Clip.antiAlias,
      child: IconButton(
        icon: const Icon(Icons.arrow_back, color: Colors.white),
        tooltip: 'Back to sign in',
        onPressed: onPressed,
      ),
    );
  }
}

/// Backend / connection failure banner shared by Login and Register.
class AuthErrorBanner extends StatelessWidget {
  final String message;

  const AuthErrorBanner({super.key, required this.message});

  static const Color _red = Color(0xFFDC2626);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: _red.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: _red.withValues(alpha: 0.45)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.error_outline, size: 18, color: _red),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              message,
              style: TextStyle(
                fontSize: 13,
                height: 1.4,
                color: Theme.of(context).colorScheme.onSurface,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
