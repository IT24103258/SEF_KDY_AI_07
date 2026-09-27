import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/request_provider.dart';
import '../../core/routes/app_router.dart';

/// My Requests screen — /my-requests  (Requester only).
///
/// Paged list of the logged-in resident's own requests.
/// Supports free-text search and status filter.
class MyRequestsScreen extends StatefulWidget {
  const MyRequestsScreen({super.key});

  @override
  State<MyRequestsScreen> createState() => _MyRequestsScreenState();
}

class _MyRequestsScreenState extends State<MyRequestsScreen> {
  final _searchCtrl = TextEditingController();

  String _statusFilter = '';
  int    _page         = 1;

  static const _pageSize = 10;

  static const _statuses = [
    '', 'Submitted', 'InReview', 'Classified',
    'PriorityAssigned', 'Matched', 'Scheduled',
    'InProgress', 'Completed', 'Cancelled',
  ];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  void _load() {
    context.read<RequestProvider>().fetchMyRequests(
          page:     _page,
          pageSize: _pageSize,
          search:   _searchCtrl.text.trim(),
          status:   _statusFilter.isEmpty ? null : _statusFilter,
        );
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'submitted':   return Colors.blue;
      case 'inreview':    return Colors.orange;
      case 'classified':  return Colors.purple;
      case 'completed':   return Colors.green;
      case 'cancelled':   return Colors.red;
      default:            return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<RequestProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Requests'),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () =>
            Navigator.pushNamed(context, AppRouter.submitRequest)
                .then((_) => _load()),
        label: const Text('New Request'),
        icon: const Icon(Icons.add),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
      ),
      body: Column(
        children: [
          // ── Filter bar ─────────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
            child: Row(children: [
              Expanded(
                child: TextField(
                  controller:  _searchCtrl,
                  decoration: const InputDecoration(
                    hintText:        'Search…',
                    prefixIcon:      Icon(Icons.search),
                    border:          OutlineInputBorder(),
                    contentPadding:  EdgeInsets.symmetric(vertical: 10),
                    isDense:         true,
                  ),
                  onSubmitted: (_) { setState(() => _page = 1); _load(); },
                ),
              ),
              const SizedBox(width: 8),
              DropdownButton<String>(
                value: _statusFilter,
                items: _statuses
                    .map((s) => DropdownMenuItem(
                        value: s, child: Text(s.isEmpty ? 'All' : s)))
                    .toList(),
                onChanged: (v) {
                  setState(() { _statusFilter = v ?? ''; _page = 1; });
                  _load();
                },
                underline: const SizedBox(),
              ),
            ]),
          ),

          // ── Results ────────────────────────────────────────────────────
          Expanded(
            child: provider.isLoading
                ? const Center(child: CircularProgressIndicator())
                : provider.error != null
                    ? Center(
                        child: Padding(
                          padding: const EdgeInsets.all(24),
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(provider.error!,
                                  textAlign: TextAlign.center,
                                  style: const TextStyle(color: Colors.red)),
                              const SizedBox(height: 12),
                              ElevatedButton(
                                  onPressed: _load,
                                  child: const Text('Retry')),
                            ],
                          ),
                        ),
                      )
                    : provider.myRequests.isEmpty
                        ? Center(
                            child: Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Icon(Icons.inbox,
                                    size: 56, color: Colors.grey),
                                const SizedBox(height: 12),
                                const Text('No requests found.',
                                    style: TextStyle(color: Colors.grey)),
                                const SizedBox(height: 8),
                                ElevatedButton(
                                  onPressed: () => Navigator.pushNamed(
                                          context, AppRouter.submitRequest)
                                      .then((_) => _load()),
                                  child: const Text('Submit your first request'),
                                ),
                              ],
                            ),
                          )
                        : RefreshIndicator(
                            onRefresh: () async => _load(),
                            child: ListView.separated(
                              padding: const EdgeInsets.fromLTRB(12, 4, 12, 80),
                              itemCount: provider.myRequests.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(height: 6),
                              itemBuilder: (context, i) {
                                final r = provider.myRequests[i];
                                return Card(
                                  elevation: 1,
                                  child: ListTile(
                                    onTap: () => Navigator.pushNamed(
                                      context,
                                      AppRouter.requestDetail,
                                      arguments: r.id,
                                    ).then((_) => _load()),
                                    leading: CircleAvatar(
                                      backgroundColor:
                                          _statusColor(r.status).withAlpha(30),
                                      child: Icon(Icons.build,
                                          color: _statusColor(r.status),
                                          size: 20),
                                    ),
                                    title: Text(r.title,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: const TextStyle(
                                            fontWeight: FontWeight.w600)),
                                    subtitle: Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.start,
                                      children: [
                                        Text(r.requestNumber,
                                            style: const TextStyle(
                                                fontSize: 11,
                                                color: Colors.grey,
                                                fontFamily: 'monospace')),
                                        if (r.category != null)
                                          Text(r.category!,
                                              style: const TextStyle(
                                                  fontSize: 12)),
                                      ],
                                    ),
                                    trailing: Column(
                                      mainAxisAlignment:
                                          MainAxisAlignment.center,
                                      crossAxisAlignment:
                                          CrossAxisAlignment.end,
                                      children: [
                                        Container(
                                          padding: const EdgeInsets.symmetric(
                                              horizontal: 8, vertical: 2),
                                          decoration: BoxDecoration(
                                            color: _statusColor(r.status)
                                                .withAlpha(30),
                                            borderRadius:
                                                BorderRadius.circular(12),
                                          ),
                                          child: Text(r.status,
                                              style: TextStyle(
                                                  fontSize: 11,
                                                  color:
                                                      _statusColor(r.status))),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          '${r.createdAt.day}/${r.createdAt.month}/${r.createdAt.year}',
                                          style: const TextStyle(
                                              fontSize: 11, color: Colors.grey),
                                        ),
                                      ],
                                    ),
                                  ),
                                );
                              },
                            ),
                          ),
          ),

          // ── Pagination ─────────────────────────────────────────────────
          if (provider.myTotalPages > 1)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  IconButton(
                    icon: const Icon(Icons.chevron_left),
                    onPressed: _page > 1
                        ? () { setState(() => _page--); _load(); }
                        : null,
                  ),
                  Text('Page $_page of ${provider.myTotalPages}',
                      style: const TextStyle(color: Colors.grey)),
                  IconButton(
                    icon: const Icon(Icons.chevron_right),
                    onPressed: _page < provider.myTotalPages
                        ? () { setState(() => _page++); _load(); }
                        : null,
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
