import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/request_provider.dart';
import '../../core/routes/app_router.dart';

/// Request Queue screen — /request-queue  (Manager / Administrator only).
///
/// Flutter mirror of React's RequestQueuePage: paged queue of ALL maintenance
/// requests via GET /api/requests, with search + status/category filters.
/// Tapping a request opens the detail screen, where classification review
/// and manual override happen.
class RequestQueueScreen extends StatefulWidget {
  const RequestQueueScreen({super.key});

  @override
  State<RequestQueueScreen> createState() => _RequestQueueScreenState();
}

class _RequestQueueScreenState extends State<RequestQueueScreen> {
  final _searchCtrl = TextEditingController();

  String _statusFilter   = '';
  String _categoryFilter = '';
  int    _page           = 1;

  static const _pageSize = 20;

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
    final provider = context.read<RequestProvider>();
    if (provider.categories.isEmpty) {
      provider.loadDropdowns();
    }
    provider.fetchAllRequests(
      page:     _page,
      pageSize: _pageSize,
      search:   _searchCtrl.text.trim(),
      status:   _statusFilter.isEmpty ? null : _statusFilter,
      category: _categoryFilter.isEmpty ? null : _categoryFilter,
    );
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'submitted':        return Colors.blue;
      case 'inreview':         return Colors.orange;
      case 'classified':       return Colors.purple;
      case 'priorityassigned':
      case 'matched':
      case 'scheduled':        return Colors.teal;
      case 'inprogress':       return Colors.amber.shade800;
      case 'completed':        return Colors.green;
      case 'cancelled':        return Colors.red;
      default:                 return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<RequestProvider>();
    final categories = provider.categories;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Request Queue'),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
      ),
      body: Column(
        children: [
          // ── Filter bar ─────────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
            child: Column(
              children: [
                Row(children: [
                  Expanded(
                    child: TextField(
                      controller: _searchCtrl,
                      decoration: const InputDecoration(
                        hintText: 'Title, description, or number…',
                        prefixIcon: Icon(Icons.search),
                        border: OutlineInputBorder(),
                        contentPadding: EdgeInsets.symmetric(vertical: 10),
                        isDense: true,
                      ),
                      onSubmitted: (_) {
                        setState(() => _page = 1);
                        _load();
                      },
                    ),
                  ),
                  const SizedBox(width: 8),
                  IconButton(
                    icon: const Icon(Icons.search),
                    tooltip: 'Search',
                    onPressed: () {
                      setState(() => _page = 1);
                      _load();
                    },
                  ),
                ]),
                const SizedBox(height: 8),
                Row(children: [
                  Expanded(
                    child: DropdownButton<String>(
                      value: _statusFilter,
                      isExpanded: true,
                      items: _statuses
                          .map((s) => DropdownMenuItem(
                              value: s, child: Text(s.isEmpty ? 'All statuses' : s)))
                          .toList(),
                      onChanged: (v) {
                        setState(() { _statusFilter = v ?? ''; _page = 1; });
                        _load();
                      },
                      underline: const SizedBox(),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: DropdownButton<String>(
                      value: _categoryFilter,
                      isExpanded: true,
                      items: [
                        const DropdownMenuItem(
                            value: '', child: Text('All categories')),
                        ...categories.map((c) => DropdownMenuItem(
                            value: c.name, child: Text(c.name))),
                      ],
                      onChanged: (v) {
                        setState(() { _categoryFilter = v ?? ''; _page = 1; });
                        _load();
                      },
                      underline: const SizedBox(),
                    ),
                  ),
                ]),
              ],
            ),
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
                    : provider.allRequests.isEmpty
                        ? const Center(
                            child: Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(Icons.inbox, size: 56, color: Colors.grey),
                                SizedBox(height: 12),
                                Text('No requests found.',
                                    style: TextStyle(color: Colors.grey)),
                                SizedBox(height: 4),
                                Text('Try adjusting your search filters.',
                                    style: TextStyle(
                                        color: Colors.grey, fontSize: 12)),
                              ],
                            ),
                          )
                        : RefreshIndicator(
                            onRefresh: () async => _load(),
                            child: ListView.separated(
                              padding: const EdgeInsets.fromLTRB(12, 4, 12, 12),
                              itemCount: provider.allRequests.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(height: 6),
                              itemBuilder: (context, i) {
                                final r = provider.allRequests[i];
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
                                      child: Icon(
                                        r.hasClassification
                                            ? Icons.auto_awesome
                                            : Icons.build,
                                        color: _statusColor(r.status),
                                        size: 20,
                                      ),
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
                                        const SizedBox(height: 2),
                                        Text(
                                          '${r.requesterName}'
                                          '${r.category != null && r.category!.isNotEmpty ? ' • ${r.category}' : ''}'
                                          '${r.hasClassification ? ' • Classified' : ''}',
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                          style: const TextStyle(fontSize: 12),
                                        ),
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
          if (provider.queueTotalPages > 1)
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
                  Text('Page $_page of ${provider.queueTotalPages}',
                      style: const TextStyle(color: Colors.grey)),
                  IconButton(
                    icon: const Icon(Icons.chevron_right),
                    onPressed: _page < provider.queueTotalPages
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
