// Time zones defined in tz database (see https://en.wikipedia.org/wiki/List_of_tz_database_time_zones).
// The list a user picks from is the platform's (api/platform/localization/timezones), the same for every admin UI;
// moment-timezone applies the picked one.
angular.module('platformWebApp')
.factory('platformWebApp.common.timeZones', ['$http', '$q', 'moment', function ($http, $q, moment) {
    // ids of the platform's list, once loaded
    var available;
    var loading;

    var toItem = function (id) {
        var utcOffset = result.utcOffset(id);
        return {
            id: id,
            utcOffset: utcOffset,
            name: '(UTC ' + utcOffset.formatted + ') ' + id
        };
    };

    var result = {
        // Any zone moment-timezone knows, case-insensitively, also one the list doesn't offer
        get: function (id) {
            var zone = id ? moment.tz.zone(id) : null;
            return zone ? toItem(zone.name) : undefined;
        },
        contains: function (id) {
            return !!(id && moment.tz.zone(id));
        },
        utcOffset: function (id) {
            var offset = moment.tz.zone(id).utcOffset(moment().valueOf());
            // UTC offset has inverted sign. Compare to zero to avoid -0
            offset = offset === 0 ? 0 : offset * -1;
            var minutes = offset % 60;
            var hours = (offset - minutes) / 60;
            // format: ±HHMM
            var pad = function (n, withSign) {
                var result = '';
                if (withSign) {
                    result = n >= 0 ? '+' : '-';
                    n = n < 0 ? n * -1 : n;
                }
                result += n < 10 ? '0' + n : n;
                return result;
            }
            return { hours: hours, minutes: minutes, formatted: pad(hours, true) + ':' + pad(minutes) };
        },
        // Loads the platform's list; without it (an older platform, a failed request) query() offers moment-timezone's zones
        load: function () {
            loading = loading || $http.get('api/platform/localization/timezones').then(function (response) {
                available = _.isArray(response.data) && response.data.length ? response.data : undefined;
            }, function () {
                loading = undefined;
            });
            return loading || $q.resolve();
        },
        query: function () {
            // Append UTC offset to name (UTC ±XX:XX Continent/City) and sort by UTC offset; zones moment-timezone doesn't know are left out
            var ids = _.filter(available || moment.tz.names(), function (x) { return !!moment.tz.zone(x); });
            return _.map(ids, toItem).sort(function (a, b) {
                return a.utcOffset.hours === b.utcOffset.hours ? a.utcOffset.minutes - b.utcOffset.minutes : a.utcOffset.hours - b.utcOffset.hours;
            });
        }
    };
    return result;
}]);
