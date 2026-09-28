import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/routes/app_router.dart';
import '../core/theme/app_colors.dart';
import '../providers/auth_provider.dart';

class AppTopBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;
  final bool showProfile;
  final bool showNotifications;
  final VoidCallback? onRefresh;
  final List<Widget>? additionalActions;

  const AppTopBar({
    super.key,
    required this.title,
    this.showProfile = true,
    this.showNotifications = true,
    this.onRefresh,
    this.additionalActions,
  });

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final initials = authProvider.user != null
        ? '${authProvider.user!.firstName.isNotEmpty ? authProvider.user!.firstName[0] : ''}${authProvider.user!.lastName.isNotEmpty ? authProvider.user!.lastName[0] : ''}'.toUpperCase()
        : 'T';

    return AppBar(
      title: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(6),
            decoration: BoxDecoration(
              color: AppColors.primary,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(Icons.build_rounded, color: Colors.white, size: 16),
          ),
          const SizedBox(width: 10),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                title,
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.bold,
                  color: isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary,
                ),
              ),
              Text(
                'FixFlow AI Mobile',
                style: TextStyle(
                  fontSize: 10,
                  fontWeight: FontWeight.w500,
                  color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                ),
              ),
            ],
          ),
        ],
      ),
      actions: [
        if (onRefresh != null)
          IconButton(
            icon: const Icon(Icons.refresh, size: 22),
            tooltip: 'Refresh',
            onPressed: onRefresh,
          ),
        if (additionalActions != null) ...additionalActions!,
        if (showNotifications)
          IconButton(
            icon: Stack(
              clipBehavior: Clip.none,
              children: [
                const Icon(Icons.notifications_outlined, size: 22),
                Positioned(
                  top: -2,
                  right: -2,
                  child: Container(
                    width: 8,
                    height: 8,
                    decoration: const BoxDecoration(
                      color: AppColors.critical,
                      shape: BoxShape.circle,
                    ),
                  ),
                ),
              ],
            ),
            tooltip: 'Notifications',
            onPressed: () {
              Navigator.pushNamed(context, AppRouter.notifications);
            },
          ),
        if (showProfile)
          Padding(
            padding: const EdgeInsets.only(right: 12, left: 4),
            child: GestureDetector(
              onTap: () {
                Navigator.pushNamed(context, AppRouter.profile);
              },
              child: CircleAvatar(
                radius: 16,
                backgroundColor: AppColors.primary,
                child: Text(
                  initials.isNotEmpty ? initials : 'T',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
          ),
      ],
    );
  }
}
